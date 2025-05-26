using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System;
using Unity.Collections;
using Steamworks;

public enum RoundType
{
    Game,
    DiscardingCards
}

public class PlayerScript : NetworkBehaviour
{
    public PlayerProfile profile;
    public PlayerUI UI;

    [SerializeField]
    private Button randomCardButton;

    [SerializeField]
    private Button discardCardButton;

    [SerializeField]
    private Button sortButton;

    [SerializeField]
    private Button endTurnButton;

    [SerializeField]
    private Button startButton;

    [SerializeField]
    private Button AddSelectedCardToMiddle;

    [SerializeField]
    private List<Card> hand;

    [SerializeField]
    private List<int> selectedCards;

    [SerializeField]
    private List<GameObject> cardsUI;

    [SerializeField]
    private GameObject cardPrefab;

    [SerializeField]
    private RectTransform cardPos;

    // For steam
    public NetworkVariable<ulong> steamId = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString128Bytes> steamName = new(writePerm: NetworkVariableWritePermission.Server);


    public NetworkVariable<bool> calledChicago = new NetworkVariable<bool>();
    public NetworkVariable<bool> hasAnsweredChicago = new NetworkVariable<bool>();

    public NetworkVariable<bool> myTurn = new(false);
    public NetworkVariable<int> points = new(0);
    public NetworkVariable<bool> isDealer = new(false);
    public NetworkVariable<int> handAmount = new(writePerm: NetworkVariableWritePermission.Owner, readPerm: NetworkVariableReadPermission.Everyone);
    public NetworkVariable<int> chicagosWon = new(0);

    public Card lastPlayedCard;

    public override void OnNetworkSpawn()
    {
        if (IsOwner && SteamClient.IsValid)
        {
            SubmitSteamDataServerRpc(SteamClient.SteamId, SteamClient.Name);
        }
    }

    [Rpc(SendTo.Server)]
    private void SubmitSteamDataServerRpc(ulong id, string name)
    {
        steamId.Value = id;
        steamName.Value = new FixedString128Bytes(name);
    }

    private void Start()
    {
        if (!IsLocalPlayer) return;

        //randomCardButton.onClick.AddListener(AddRandomCard);
        discardCardButton.onClick.AddListener(DiscardCards);
        sortButton.onClick.AddListener(Check);
        endTurnButton.onClick.AddListener(EndTurn);

        AddSelectedCardToMiddle.gameObject.SetActive(false);
        AddSelectedCardToMiddle.onClick.AddListener(AddCardToMiddle);

        discardCardButton.gameObject.SetActive(false);
        endTurnButton.gameObject.SetActive(false);

        myTurn.OnValueChanged += SetActiveTurnRpc;


        if (IsServer)
        {
            startButton.gameObject.SetActive(true);
            startButton.onClick.AddListener(StartGame);
        }
    }

    private void AddCardToMiddle()
    {
        if (selectedCards.Count <= 0) return;

        int value = hand[selectedCards[0]].value;
        Suit suit = hand[selectedCards[0]]._suit;

        GameManager.GM.PlayCardRpc(value, suit, profile.SeatId.Value);
        lastPlayedCard = hand[selectedCards[0]];

        ResetCardSelection();
        DiscardCards();
        EndTurnRpc();
        UpdateCardAmountForUI();
    }

    void StartGame()
    {
        startButton.gameObject.SetActive(false);
        GameManager.GM.StartGameRpc();
    }

    void Check()
    {
        HandDetector sortedHand = new(hand);

        print(sortedHand.CheckHand());
    }

    [Rpc(SendTo.Owner)]
    public void SetActiveTurnRpc(bool previousValue, bool newValue)
    {
        selectedCards.Clear();

        UI.WaitingForChicagoUIRpc(false);

        if (newValue && roundType == RoundType.DiscardingCards)
        {
            //discardCardButton.gameObject.SetActive(true);
            endTurnButton.gameObject.SetActive(true);
            endTurnButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Done");
            UI.chooseCardsUI.SetActive(true);
        }
        else if(newValue && roundType == RoundType.Game)
        {
            //AddSelectedCardToMiddle.gameObject.SetActive(true);
            endTurnButton.gameObject.SetActive(true);
            endTurnButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Play Card");
            UI.yourTurnUI.SetActive(true);

            //CheckPlayableCards();
        }
        else
        {
            //AddSelectedCardToMiddle.gameObject.SetActive(false);

            //discardCardButton.gameObject.SetActive(false);
            //endTurnButton.gameObject.SetActive(false);
        }
    }
    void EndTurn()
    {
        UI.yourTurnUI.SetActive(false);
        UI.WaitingForChicagoUIRpc(true);
        if (roundType == RoundType.Game)
        {
            AddCardToMiddle();
        }
        else
        {
            UI.chooseCardsUI.SetActive(false);
            DiscardCards();
            EndTurnRpc();
        }
        
    }

    [Rpc(SendTo.Server)]
    public void EndTurnRpc()
    {
        myTurn.Value = false;
    }

    [Rpc(SendTo.Owner)]
    public void CheckPlayableCardsRpc(int currentSuit)
    {
        List<int> playableCards = new();

        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i]._suit == (Suit)currentSuit)
            {
                playableCards.Add(i);
            }
        }

        if (playableCards.Count > 0)
        {

            for (int i = 0; i < cardsUI.Count; i++)
            {
                var cardScript = cardsUI[i].GetComponent<CardUI>();
                if (cardScript == null) continue;
                if (playableCards.Contains(i))
                {
                    cardScript.CanSelect(true);
                    cardScript.Outline(true);
                }
                else
                {
                    cardScript.CanSelect(false);
                    cardScript.Outline(false);
                }
            }

        }
    }

    void ResetCardSelection()
    {
        foreach (var item in cardsUI)
        {
            var cardScript = item.GetComponent<CardUI>();
            if (cardScript == null) return;

            cardScript.CanSelect(true);
            cardScript.Outline(false);
        }
    }

    void UpdateCardUI(Card card, int index)
    {
       
        GameObject cardUI = Instantiate(cardPrefab, GetComponentInChildren<Canvas>().transform);
        cardUI.transform.parent = cardPos.transform;

        RectTransform rectTransform = cardUI.GetComponent<RectTransform>();

        rectTransform.position = cardPos.position;

        cardUI.GetComponent<RawImage>().texture = Resources.Load<Texture2D>("MyCards/Cards/" + card.FileName());
        cardUI.GetComponent<CardUI>().playerScript = this;
        cardUI.GetComponent<CardUI>().cardIndex = index;

        cardsUI.Add(cardUI);

        //UpdateCardPos();
    }

    [Rpc(SendTo.Server)]
    public void AddRandomCardRpc()
    {
        if (hand.Count >= GameRules.maxHandAmount)
        {
            print("You already have 5 cards!");
            return;
        }

        print("Adding " + (GameRules.maxHandAmount - hand.Count).ToString() + " amount of cards");

        List<Card> tempHand = new();

        for (int i = 0; i < (GameRules.maxHandAmount - hand.Count); i++)
        {
            Card card = CardDeck.instance.GetRandomCard();

            tempHand.Add(card);
            if (IsLocalPlayer)
            {
                UpdateCardUI(card, i);
            }
            

            AddRandomCardClientRpc(card.value, card._suit, i);
            
        }

        hand.AddRange(tempHand);

        UpdateCardPosRpc();

    }

    [Rpc(SendTo.Owner)]
    public void AddRandomCardClientRpc(int value, Suit suit, int index)
    {
        if (IsServer) return;

        Card card = new(value, suit);
        UpdateCardUI(card, index);
        hand.Add(card);
    }

    void DiscardCards()
    {

        if (IsServer)
        {
            DoDiscard(selectedCards.ToArray());
        }
        else
        {
            DiscardCardsServerRpc(selectedCards.ToArray());
        }

        selectedCards.Clear();
        UpdateCardAmountForUI();
    }

    [Rpc(SendTo.Server)]
    void DiscardCardsServerRpc(int[] indices)
    {
        DoDiscard(indices);
    }

    void DoDiscard(int[] indices)
    {
        List<int> sortedIndices = new(indices);
        sortedIndices.Sort();
        sortedIndices.Reverse(); // descending

        foreach (int i in sortedIndices)
        {
            if (i < hand.Count) hand.RemoveAt(i);
            if (i < cardsUI.Count)
            {
                Destroy(cardsUI[i]);
                cardsUI.RemoveAt(i);
            }
        }

        UpdateCardPosRpc();

        DoDiscardClientRpc(indices);
    }

    [Rpc(SendTo.Owner)]
    void DoDiscardClientRpc(int[] indices)
    {
        if (IsServer) return;

        List<int> sortedIndices = new(indices);
        sortedIndices.Sort();
        sortedIndices.Reverse();

        foreach (int i in sortedIndices)
        {
            if (i < hand.Count) hand.RemoveAt(i);
            if (i < cardsUI.Count)
            {
                Destroy(cardsUI[i]);
                cardsUI.RemoveAt(i);
            }
        }

        
        UpdateCardPosRpc();
    }

    [Rpc(SendTo.Owner)]
    public void DiscardAllCardsRpc()
    {
        List<int> allCards = new();

        for (int i = 0; i < hand.Count; i++)
        {
            allCards.Add(i);
        }

        if (IsServer)
        {
            DoDiscard(allCards.ToArray());
        }
        else
        {
            DiscardCardsServerRpc(allCards.ToArray());
        }

        selectedCards.Clear();
    }


    /*  [Rpc(SendTo.Server)]
      void DiscardCardsServerRpc(int index)
      {
          this.hand.RemoveAt(index);
          if (cardsUI[index] != null) 
          {
              Destroy(cardsUI[index]);
              cardsUI.RemoveAt(index);
          }
      }*/

    public void SelectCard(int value)
    {
        if (!myTurn.Value) return;

        // If round is game, clearing cards ensuring no previous cards are in the list


        if (roundType == RoundType.Game)
        {
            selectedCards.Clear();

            for (int i = 0; i < cardsUI.Count; i++)
            {
                if (i == value) continue;
                var cardsScript = cardsUI[i].GetComponent<CardUI>();
                if (cardsScript == null) return;
                cardsScript.SelectCard(false);
            }
        }

        selectedCards.Add(value);

        if (roundType == RoundType.DiscardingCards)
        {
            if (selectedCards.Count > 0)
            {
                endTurnButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Discard");
            }
        }
    }

    public void UnselectCard(int value)
    {
        selectedCards.Remove(value);

        if (roundType == RoundType.DiscardingCards && selectedCards.Count < 0)
        {
            endTurnButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Done");
        }
    }

    [Rpc(SendTo.Owner)]
    void UpdateCardPosRpc()
    {
        for (int i = 0; i < cardsUI.Count; i++)
        {
            cardsUI[i].GetComponent<CardUI>().cardIndex = i;
            cardsUI[i].GetComponent<CardUI>().UpdateXPos();
            //cardsUI[i].GetComponent<CardUI>().UpdateXPos(i);
        }
        UpdateCardAmountForUI();
    }



    private RoundType roundType;
    [Rpc(SendTo.Owner)]
    public void SetRoundTypeRpc(RoundType roundType)
    {
        this.roundType = roundType;
    }

    public List<Card> GetHand()
    {
        return hand;
    }

    void UpdateCardAmountForUI()
    {
        handAmount.Value = hand.Count;
    }
}
