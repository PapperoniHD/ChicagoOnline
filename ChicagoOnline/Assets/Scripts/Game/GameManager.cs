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

    //[SerializeField]
    //private int currentSuit;
    public NetworkVariable<int> currentSuit = new();
    [SerializeField]
    private Card lastPlayedCard;

    public List<GameObject> cardsOnTable = new();

    // Seats
    private readonly int[] seatOrder = { 0, 2, 1, 3 };
    private int nextSeatIndex = 0;

    private bool showingAnnouncement = false;
    private Queue<(string text, float duration)> announcementQueue = new();

    private void Awake()
    {
        if (GM == null) GM = this;
        else Destroy(gameObject);
    }

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
        players[dealerPlayerIndex].isDealer.Value = true;
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

        List<PlayerScript> turnOrder = GetTurnOrderBySeat();

        // Get dealer's seat
        int dealerSeat = players[dealerPlayerIndex].profile.SeatId.Value;

        // Find who is next after dealer in turn order
        int startingIndex = turnOrder.FindIndex(p => p.profile.SeatId.Value == dealerSeat);
        startingIndex = (startingIndex + 1) % turnOrder.Count;

        int currentTurnPlayerWinnerIndex = 0;



        bool someoneCalledChicago = false;
        PlayerScript chicagoPlayer = null;

        // Ask everyone if they want to call chicago
        for (int j = 0; j < turnOrder.Count; j++)
        {
            int index = (startingIndex + j) % turnOrder.Count;
            PlayerScript currentPlayer = turnOrder[index];

            foreach (var player in players)
            {
                if (player == currentPlayer) continue;
                player.UI.WaitingForChicagoUIRpc(true);
            }
            currentPlayer.UI.AskForChicagoRpc();


            while (!currentPlayer.hasAnsweredChicago.Value)
            {
                yield return null;
            }

            foreach (var player in players)
            {
                player.UI.WaitingForChicagoUIRpc(false);
            }

            if (currentPlayer.calledChicago.Value)
            {
                background.ChangeToChicagoRpc();
                someoneCalledChicago = true;
                chicagoPlayer = currentPlayer;
                yield return StartCoroutine(ShowAnnouncementText($"{currentPlayer.GetName()} called CHICAGO!", 2f));
                yield return new WaitForSeconds(0.5f);
                break;
            }
        }

        if (someoneCalledChicago)
        {
            startingIndex = turnOrder.IndexOf(chicagoPlayer);

            for (int round = 0; round < 5; round++)
            {
                int cachedCardValue = 0;
                currentTurnPlayerWinnerIndex = startingIndex;

                for (int j = 0; j < turnOrder.Count; j++)
                {
                    int index = (startingIndex + j) % turnOrder.Count;
                    PlayerScript currentPlayer = turnOrder[index];

                    if (j != 0)
                    {
                        currentPlayer.CheckPlayableCardsRpc(currentSuit.Value);
                    }

                    foreach (var player in players)
                    {
                        if (player == currentPlayer) continue;
                        player.UI.WaitingForChicagoUIRpc(true);
                    }
                    currentPlayer.myTurn.Value = true;

                    while (currentPlayer.myTurn.Value)
                    {
                        yield return null;
                    }

                    foreach (var player in players)
                    {
                        player.UI.WaitingForChicagoUIRpc(false);
                    }

                    if (j == 0)
                    {
                        currentSuit.Value = (int)lastPlayedCard._suit;
                        cachedCardValue = lastPlayedCard.value == 1 ? 14 : lastPlayedCard.value;
                        currentTurnPlayerWinnerIndex = index;
                    }
                    else if ((int)lastPlayedCard._suit == currentSuit.Value)
                    {
                        int lastCardValue = lastPlayedCard.value == 1 ? 14 : lastPlayedCard.value;
                        if (lastCardValue > cachedCardValue)
                        {
                            cachedCardValue = lastCardValue;
                            currentTurnPlayerWinnerIndex = index;
                        }
                    }
                }

                // Check if someone beat the chicagoPlayer
                if (turnOrder[currentTurnPlayerWinnerIndex] != chicagoPlayer)
                {
                    PlayLoseSound();
                    yield return StartCoroutine(ShowAnnouncementText($"{turnOrder[currentTurnPlayerWinnerIndex].GetName()} broke the CHICAGO! Seat {chicagoPlayer.profile.SeatId.Value} loses 15 points.", 3f));             
                    chicagoPlayer.points.Value -= GameRules.ChicagoPoints;
                    yield break;
                }

                // Chicago player won the trick, they start next
                startingIndex = turnOrder.IndexOf(chicagoPlayer);
            }

            // If all 5 tricks are won by chicagoPlayer
            PlayWinSound();
            yield return StartCoroutine(ShowAnnouncementText($"{chicagoPlayer.GetName()} successfully completed CHICAGO and gets 15 points!", 3f));
            chicagoPlayer.points.Value += GameRules.ChicagoPoints;
        }
        else
        {
            // Normal play, no chicago called
            PlayNoChicagoSound();
            yield return StartCoroutine(ShowAnnouncementText($"No CHICAGO was called.", 1.5f)); 
            var (cachedWinner, cachedWinnerHand) = GetWinner();
            int finalTrickWinningCardValue = 0;
            for (int round = 0; round < 5; round++)
            {
                int cachedCardValue = 0;
                currentTurnPlayerWinnerIndex = startingIndex;

                for (int j = 0; j < turnOrder.Count; j++)
                {
                    int index = (startingIndex + j) % turnOrder.Count;
                    PlayerScript currentPlayer = turnOrder[index];

                    // Check playable cards, skip first player as that player controls the cards
                    if (j != 0)
                    {
                        currentPlayer.CheckPlayableCardsRpc(currentSuit.Value);
                    }

                    foreach (var player in players)
                    {
                        if (player == currentPlayer) continue;
                        player.UI.WaitingForChicagoUIRpc(true);
                    }

                    currentPlayer.myTurn.Value = true;

                    while (currentPlayer.myTurn.Value)
                    {
                        yield return null;
                    }

                    foreach (var player in players)
                    {
                        player.UI.WaitingForChicagoUIRpc(false);
                    }

                    if (j == 0)
                    {
                        currentSuit.Value = (int)lastPlayedCard._suit;
                        cachedCardValue = lastPlayedCard.value == 1 ? 14 : lastPlayedCard.value;
                        currentTurnPlayerWinnerIndex = index;

                        if (round == 4)
                        {
                            finalTrickWinningCardValue = lastPlayedCard.value;
                        }
                    }
                    else if ((int)lastPlayedCard._suit == currentSuit.Value)
                    {
                        int lastCardValue = lastPlayedCard.value == 1 ? 14 : lastPlayedCard.value;
                        if (lastCardValue > cachedCardValue)
                        {
                            cachedCardValue = lastCardValue;
                            currentTurnPlayerWinnerIndex = index;

                            if (round == 4)
                            {
                                finalTrickWinningCardValue = lastCardValue;
                            }
                        }


                    }
                }

                // Winner starts next round
                if (round == 4)
                {
                    PlayerScript winner = turnOrder[currentTurnPlayerWinnerIndex];
                    PlayNoChicagoSound();
                    yield return StartCoroutine(ShowAnnouncementText($"{winner.GetName()} won the round, and gets {GameRules.roundWin_Points} points.", 3f));
                    winner.points.Value += GameRules.roundWin_Points;

                    if (finalTrickWinningCardValue == 2)
                    {
                        PlayNoChicagoSound();
                        yield return StartCoroutine(ShowAnnouncementText($"{winner.GetName()} ended the round with a TWO, and gets additional {GameRules.roundWin_Points} points.", 3f));
                        winner.points.Value += GameRules.roundWin_Points;
                    }

                    if (cachedWinner != null)
                    {
                        PlayNoChicagoSound();
                        yield return StartCoroutine(ShowAnnouncementText($"{cachedWinner.GetName()} has {cachedWinnerHand} and gets {GameRules.handPoints[cachedWinnerHand]} points.", 3f));

                        cachedWinner.points.Value += GameRules.handPoints[cachedWinnerHand];
                        cachedWinner.chicagosWon.Value++;
                    }
                    
                    /*if (cachedWinnerHand)
                    {

                    }*/

                    while (showingAnnouncement)
                    {
                        yield return null;
                    }
                }

                startingIndex = currentTurnPlayerWinnerIndex;
            }
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

            yield return StartCoroutine(ShowAnnouncementText($"{currentWinner.GetName()} has the highest {highestHand} and gets {GameRules.handPoints[highestHand]} points.", 3f));
            //QueueAnnouncementText($"Seat {currentWinner.profile.SeatId.Value} has {highestHand} and gets {GameRules.handPoints[highestHand]} points", 3f);

            // Unused, wanted to do queues but was not really necessesary
            /*while (showingAnnouncement)
            {
                yield return null;
            }*/

            currentWinner.points.Value += GameRules.handPoints[highestHand];
        }
        yield return null;
    }

    (PlayerScript, Hands hand) GetWinner()
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

        return (currentWinner, highestHand);
    }

    public IEnumerator EndRoundCleanup()
    {
        yield return new WaitForSeconds(1f);

        CleanUpRpc();
        deck.InitializeDeck();

        // Reset chicago
        foreach (var player in players)
        {
            player.calledChicago.Value = false;
            player.hasAnsweredChicago.Value = false;
            player.DiscardAllCardsRpc();
        }

        yield return StartCoroutine(ShowAnnouncementText($"New Round!", 1f));

        AdvanceDealer();

        yield return new WaitForSeconds(1f);
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
    public void PlayCardRpc(int value, Suit suit, int seatId)
    {
        lastPlayedCard = new(value, suit);

        AddCardToTableRpc(value, suit, seatId);
    }

    [Rpc(SendTo.Everyone)]
    public void AddCardToTableRpc(int value, Suit suit, int seatId)
    {
        GameObject cardUI = Instantiate(cardPrefab);

        Transform parent = GameUI.Instance.GetCardParent(seatId);
        if (parent == null)
        {
            Debug.LogError($"Card parent is null for seatId {seatId}");
            return;
        }

        cardUI.transform.SetParent(parent, false); // false = retain local layout settings
        RectTransform rectTransform = cardUI.GetComponent<RectTransform>();
        rectTransform.localPosition = Vector3.zero;

        Quaternion randomZRotation = Quaternion.Euler(0, 0, Random.Range(-30f, 30f));
        rectTransform.localRotation = randomZRotation;

        cardUI.GetComponent<RawImage>().texture =
            Resources.Load<Texture2D>("MyCards/Cards/" + (Suit)suit + value);

        cardsOnTable.Add(cardUI);

        PlayerSound.instance.PlayPlaceCard();
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

    void QueueAnnouncementText(string text, float duration)
    {
        announcementQueue.Enqueue((text, duration));

        if (!showingAnnouncement)
        {
            StartCoroutine(ShowAnnouncementText());
        }
    }

    IEnumerator ShowAnnouncementText()
    {
        showingAnnouncement = true;

        while (announcementQueue.Count > 0)
        {
            var (text, duration) = announcementQueue.Dequeue();

            foreach (var item in players)
            {
                item.GetComponentInChildren<PlayerUI>().AnnouncementTextRpc(text);
            }

            yield return new WaitForSeconds(duration);

            foreach (var item in players)
            {
                item.GetComponentInChildren<PlayerUI>().HideTextRpc();
            }

            yield return new WaitForSeconds(0.2f);
        }

        showingAnnouncement = false;
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
        yield return new WaitForSeconds(1f);
    }

    void PlayWinSound()
    {
        foreach (var item in players)
        {
            item.PlayWinChicagoSoundRpc();
        }
    }

    void PlayLoseSound()
    {
        foreach (var item in players)
        {
            item.PlayLoseChicagoSoundRpc();
        }
    }

    void PlayNoChicagoSound()
    {
        foreach (var item in players)
        {
            item.PlayNoChicagoSoundRpc();
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

    public PlayerScript GetPlayerBySeat(int seatId)
    {
        foreach (var p in players)
        {
            if (p.profile.SeatId.Value == seatId)
                return p;
        }
        return null;
    }

    private List<PlayerScript> GetTurnOrderBySeat()
    {
        List<PlayerScript> ordered = new();

        int[] clockwiseSeats = { 0, 1, 2, 3 };

        foreach (int seatId in clockwiseSeats)
        {
            foreach (var p in players)
            {
                if (p.profile.SeatId.Value == seatId)
                {
                    ordered.Add(p);
                    break;
                }
            }
        }

        return ordered;
    }

    private void AdvanceDealer()
    {
        List<int> occupiedSeats = new();
        foreach (var player in players)
        {
            player.isDealer.Value = false;
            occupiedSeats.Add(player.profile.SeatId.Value);
        }
        occupiedSeats.Sort();

        int currentDealerSeat = players[dealerPlayerIndex].profile.SeatId.Value;

        int currentIndex = occupiedSeats.IndexOf(currentDealerSeat);

        int nextIndex = (currentIndex + 1) % occupiedSeats.Count;
        int nextDealerSeat = occupiedSeats[nextIndex];

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].profile.SeatId.Value == nextDealerSeat)
            {
                dealerPlayerIndex = i;
                players[i].isDealer.Value = true;
                break;
            }
        }
    }

}
