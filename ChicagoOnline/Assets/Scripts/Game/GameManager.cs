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
    public CardDeck deck;
    public BackgroundManager background;
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

    public List<GameObject> cardsOnTable = new();

    // Seats
    private readonly int[] seatOrder = { 0, 2, 1, 3 };
    private int nextSeatIndex = 0;

    private void Awake()
    {
        if (GM == null) GM = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Called on the server when a client joins. Returns a seatId in [0..3].
    /// </summary>
    public int AssignSeatId(ulong clientId)
    {
        if (nextSeatIndex < seatOrder.Length)
        {
            return seatOrder[nextSeatIndex++];
        }
        else
        {
            // Fallback if you ever exceed 4 players
            return nextSeatIndex++;
        }
    }


    private IEnumerator StartGame()
    {
        /*foreach (var item in players)
        {
            item.GetComponentInChildren<PlayerUI>().ExplosiveTextRpc("Starting game...");
            for (int i = 0; i < players.Count; i++)
            {
                ulong networkObjectID = players[i].NetworkObjectId; 
                item.GetComponentInChildren<PlayerUI>().AddPlayerScoreObjectClientRpc(networkObjectID, i);
            }      
        }*/
        yield return new WaitForSeconds(1);
        StartCoroutine(GameLoop());
    }

    private IEnumerator GameLoop()
    {
        while (AllPlayersBelowMaxScore())
        {
            yield return StartCoroutine(DiscardPhase());
            yield return StartCoroutine(TrickTakingPhase());
            yield return StartCoroutine(EndRoundCleanup());
            yield return null;
        }

        background.ChangeToLobbyRpc();

    }

    private IEnumerator DiscardPhase()
    {
        currentState = GameState.DiscardingCards;
        background.ChangeToDiscardRpc();

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
                yield return StartCoroutine(CheckWinner());
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

        //StartCoroutine(TrickTakingPhase());

    }

    private IEnumerator TrickTakingPhase()
    {
        currentState = GameState.PlayingTrick;
        background.ChangeToTrickTakingRpc();

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

    IEnumerator CheckWinner()
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

            yield return StartCoroutine(ShowAnnouncementText($"Player {playerIndex + 1} has {highestHand} and gets {GameRules.handPoints[highestHand]} points", 3f));

            currentWinner.points.Value += GameRules.handPoints[highestHand];
        }
        yield return null;
    }

    public IEnumerator EndRoundCleanup()
    {
        yield return new WaitForSeconds(1f);

        CleanUpRpc();
        deck.InitializeDeck();

        yield return new WaitForSeconds(3f);
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

        cardsOnTable.Add(cardUI);
        cardUI.GetComponent<RawImage>().texture = Resources.Load<Texture2D>("MyCards/Cards/" + (Suit)suit + value);
        cardUI.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, Random.Range(-30f, 30f));
    }

    [Rpc(SendTo.Everyone)]
    void CleanUpRpc()
    {
        StartCoroutine(RemoveCardsFromTable());
    }
        
    IEnumerator RemoveCardsFromTable()
    {
        foreach (var item in cardsOnTable)
        {
            RectTransform card = item.GetComponent<RectTransform>();
            StartCoroutine(RemoveAndDestroy(card));

            yield return new WaitForSeconds(0.3f);
        }

        cardsOnTable.Clear();
    }

    IEnumerator RemoveAndDestroy(RectTransform card)
    {
        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            card.anchoredPosition += Vector2.right.normalized * 1000f * Time.deltaTime;
            yield return null;
        }

        Destroy(card.gameObject);
    }

    void SendExplosiveTextToEveryone(string text)
    {
        foreach (var item in players)
        {
            item.GetComponentInChildren<PlayerUI>().ExplosiveTextRpc(text);        
        }
    }

    IEnumerator ShowAnnouncementText(string text, float duration)
    {
        foreach (var item in players)
        {
            item.GetComponentInChildren<PlayerUI>().AnnouncementTextRpc(text);
        }

        yield return new WaitForSeconds(duration);

        foreach (var item in players)
        {
            item.GetComponentInChildren<PlayerUI>().HideTextRpc();
        }

    }

    private bool AllPlayersBelowMaxScore(int maxScore = 52)
    {
        foreach (var player in players)
        {
            if (player.points.Value >= maxScore)
            {
                return false;
            }
        }
        return true;
    }


}
