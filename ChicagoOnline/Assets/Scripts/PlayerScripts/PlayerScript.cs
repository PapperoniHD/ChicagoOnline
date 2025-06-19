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

    [Header("Card Data")]
    [SerializeField]
    private List<Card> hand;
    public Card lastPlayedCard;
    [SerializeField] private List<int> selectedCards;
    [SerializeField] private List<GameObject> cardsUI;

    
    [Header("Network Variables")]
    public NetworkVariable<bool> calledChicago = new NetworkVariable<bool>();
    public NetworkVariable<bool> hasAnsweredChicago = new NetworkVariable<bool>();

    public NetworkVariable<bool> myTurn = new(false);
    public NetworkVariable<int> points = new(0);
    public NetworkVariable<bool> isDealer = new(false);
    public NetworkVariable<int> handAmount = new(writePerm: NetworkVariableWritePermission.Owner, readPerm: NetworkVariableReadPermission.Everyone);
    public NetworkVariable<int> chicagosWon = new(0);

    private void Start()
    {
        myTurn.OnValueChanged += SetActiveTurnRpc;
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
        UI.endTurnButton.gameObject.SetActive(false);
        
    }

    public void StartGame()
    {
        UI.startButton.gameObject.SetActive(false);
        GameManager.GM.StartGameRpc();
    }

    public void Check()
    {
        HandDetector sortedHand = new(hand);

        print(sortedHand.CheckHand());
    }

    [Rpc(SendTo.Owner)]
    public void SetActiveTurnRpc(bool previousValue, bool newValue)
    {
        selectedCards.Clear();

        if (newValue && roundType == RoundType.DiscardingCards)
        {
            UI.WaitingForChicagoUIRpc(false);
            UI.endTurnButton.gameObject.SetActive(true);
            UI.endTurnButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Done");
            UI.chooseCardsUI.SetActive(true);
        }
        else if(newValue && roundType == RoundType.Game)
        {
            UI.WaitingForChicagoUIRpc(false);
            UI.endTurnButton.gameObject.SetActive(true);
            UI.endTurnButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Play Card");
            UI.yourTurnUI.SetActive(true);

        }
        else
        {
            UI.endTurnButton.gameObject.SetActive(false);
        }
    }
    public void EndTurn()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayButton();
        }

        UI.yourTurnUI.SetActive(false);
        UI.WaitingForChicagoUIRpc(true);

        if (roundType == RoundType.Game)
        {
            AddCardToMiddle();
        }
        else
        {

            UI.endTurnButton.gameObject.SetActive(false);
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
        GameObject cardUI = Instantiate(UI.cardPrefab, GetComponentInChildren<Canvas>().transform);
        cardUI.transform.parent = UI.cardPos.transform;

        RectTransform rectTransform = cardUI.GetComponent<RectTransform>();

        rectTransform.position = UI.cardPos.position;

        cardUI.GetComponent<RawImage>().texture = Resources.Load<Texture2D>("MyCards/Cards/" + card.FileName());
        cardUI.GetComponent<CardUI>().playerScript = this;
        cardUI.GetComponent<CardUI>().cardIndex = index;

        cardsUI.Add(cardUI);
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

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayGetCards();
        }
    }

    public void DiscardCards()
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
    void DiscardCardsServerRpc(int[] selected)
    {
        DoDiscard(selected);
    }

    void DoDiscard(int[] selected)
    {
        Discard(selected);

        UpdateCardPosRpc();
        DoDiscardClientRpc(selected);
    }

    [Rpc(SendTo.Owner)]
    void DoDiscardClientRpc(int[] selected)
    {
        if (IsServer) return;

        Discard(selected);   
        UpdateCardPosRpc();
    }

    private void Discard(int[] selected)
    {
        List<int> sortedSelected = new(selected);
        sortedSelected.Sort();
        sortedSelected.Reverse();

        foreach (int i in sortedSelected)
        {
            if (i < hand.Count) hand.RemoveAt(i);
            if (i < cardsUI.Count)
            {
                Destroy(cardsUI[i]);
                cardsUI.RemoveAt(i);
            }
        }
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
                UI.endTurnButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Discard");
            }
        }
    }

    

    public void UnselectCard(int value)
    {
        selectedCards.Remove(value);

        if (roundType == RoundType.DiscardingCards && selectedCards.Count <= 0)
        {
            UI.endTurnButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Done");
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
