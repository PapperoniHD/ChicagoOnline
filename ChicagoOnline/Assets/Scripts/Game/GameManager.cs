using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public enum GameState
{
    DiscardingCards,
    PlayingTrick,
    RoundEnd
}

public class GameManager : NetworkBehaviour
{
    public static GameManager GM;
    public GameState currentState;

    int turnCount = GameRules.turnCount;

    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject cardPrefab;

    public NetworkVariable<int> currentTurn { get; private set; } = new();

    bool turnActive = false;

    public List<PlayerScript> players;

    private int dealerPlayerIndex = 0;

    private int turnPlayerIndex = 0;

    [SerializeField]
    private Suit currentSuit;
    [SerializeField]
    private Card lastPlayedCard;

    private void Awake()
    {
        GM = this;
    }

    

    private IEnumerator StartGame()
    {
        foreach (var item in players)
        {
            item.GetComponentInChildren<PlayerUI>().ExplosiveTextRpc("Starting game...");
            for (int i = 0; i < players.Count; i++)
            {
                ulong networkObjectID = players[i].NetworkObjectId; 
                item.GetComponentInChildren<PlayerUI>().AddPlayerScoreObjectClientRpc(networkObjectID, i);
            }
            
        }
        yield return new WaitForSeconds(1);
        StartCoroutine(DiscardPhase());
    }

    private IEnumerator DiscardPhase()
    {
        currentState = GameState.DiscardingCards;
       
        // Give initial cards
        foreach (var item in players)
        {
            item.SetRoundTypeRpc(RoundType.DiscardingCards);
            yield return new WaitForSeconds(0.5f);
            item.AddRandomCardRpc();
        }

        // Starting rounds
        for (int i = 0; i < turnCount; i++)
        {
            if (i > 0)
            {
                CheckWinner();
                yield return new WaitForSeconds(1);
            }
           
            foreach (var item in players)
            {
                turnActive = true;

                item.myTurn.Value = true;

            }

            // Don't continue until all players are done
            foreach (var item in players)
            {
                while (item.myTurn.Value)
                {
                    yield return null;
                }
            }

            currentTurn.Value++;


            // Give each players cards again
            foreach (var item in players)
            {
                item.AddRandomCardRpc();
            }

        }

        StartCoroutine(TrickTakingPhase());

    }

    private IEnumerator TrickTakingPhase()
    {
        currentState = GameState.PlayingTrick;

        // Call game start
        foreach (var item in players)
        {
            item.GetComponentInChildren<PlayerUI>().ExplosiveTextRpc("GAME START!");
            item.SetRoundTypeRpc(RoundType.Game);
        }

        yield return new WaitForSeconds(2f);

        turnPlayerIndex = (dealerPlayerIndex + 1) % players.Count;
        print(turnPlayerIndex);

        // Put chicago turn here
        int currentTurnPlayerWinner = 0;

        for (int i = 0; i < 5; i++)  // 5 rounds of trick-taking
        {
            int cachedCardValue = 0;
            currentTurnPlayerWinner = turnPlayerIndex; // Set first player as default winner

            // First player starts the trick
            for (int j = 0; j < players.Count; j++)
            {
                players[turnPlayerIndex].myTurn.Value = true;
                while (players[turnPlayerIndex].myTurn.Value)
                {
                    yield return null;
                }

                // First card played determines the suit
                if (j == 0)
                {
                    currentSuit = lastPlayedCard._suit;
                    cachedCardValue = lastPlayedCard.value == 1 ? 14 : lastPlayedCard.value; // Treat Ace as 14
                    currentTurnPlayerWinner = turnPlayerIndex;
                }
                else if (lastPlayedCard._suit == currentSuit)
                {
                    int lastCardValue = lastPlayedCard.value == 1 ? 14 : lastPlayedCard.value; // Treat Ace as 14

                    if (lastCardValue > cachedCardValue)
                    {
                        cachedCardValue = lastCardValue;
                        currentTurnPlayerWinner = turnPlayerIndex;
                    }
                }

                turnPlayerIndex = (turnPlayerIndex + 1) % players.Count;
            }

            // Winner starts the next trick
            turnPlayerIndex = currentTurnPlayerWinner;
        }


    }

    void CheckWinner()
    {
        Hands highestHand = Hands.Nothing;
        PlayerScript currentWinner = null;
        int playerIndex = 0;

        for (int i = 0; i < players.Count; i++)
        {
            HandDetector Hand = new(players[i].GetHand());
            Hands playerHand = Hand.CheckHand();

            if (playerHand < highestHand)
            {
                highestHand = playerHand;
                currentWinner = players[i];
                playerIndex = i;
            }
        }

        if (currentWinner)
        {
            print("Try send explosive text");
            SendExplosiveTextToEveryone("Player " + (playerIndex+1) + "has " + highestHand.ToString() + " and gets " + GameRules.handPoints[highestHand] + " points");
            currentWinner.points.Value += GameRules.handPoints[highestHand];
        }
    }

    [Rpc(SendTo.Server)]
    public void StartGameRpc()
    {

        foreach (var item in NetworkManager.Singleton.ConnectedClientsList)
        {
            players.Add(item.PlayerObject.GetComponent<PlayerScript>());
        }

        currentState = GameState.DiscardingCards;
        StartCoroutine(StartGame());
    }

    [Rpc(SendTo.Server)]
    public void AddCardToMiddleServerRpc(int value, Suit suit)
    {
        lastPlayedCard = new(value, suit);

        AddCardToMiddleClientRpc(value, suit);
    }

    [Rpc(SendTo.Everyone)]
    public void AddCardToMiddleClientRpc(int value, Suit suit)
    {
        GameObject cardUI = Instantiate(cardPrefab, canvas.transform);
        cardUI.transform.parent = canvas.transform;

        // cardUI.GetComponent<RectTransform>().position = Vector3.zero;
        cardUI.GetComponent<RawImage>().texture = Resources.Load<Texture2D>("Playing Cards/Image/PlayingCards/" + (Suit)suit + value);

        cardUI.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, Random.Range(-30f, 30f));

    }

    void SendExplosiveTextToEveryone(string text)
    {
        foreach (var item in players)
        {
            item.GetComponentInChildren<PlayerUI>().ExplosiveTextRpc(text);        
        }
    }

}
