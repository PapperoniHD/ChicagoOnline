# CHICAGO ONLINE
#### Video Demo:  https://www.youtube.com/watch?v=dBqNXkimrMA
#### Description:

For my final project, I decided to make a online version of a Swedish poker variant called "Chicago". Inspired by Balatro.

### About Chicago

Chicago is a tactical poker variant where for three rounds, you gather and discard cards, trying to get the best hands as possible, and ends with a trick-taking phase where you play for the win.
The goal of the game is to get 51 points, as well as have won one round of called "Chicago". Every player gets 5 cards.

#### Discarding

During the three discarding rounds, players can also get points if you have the current highest hand, which then you would also have to reveal what hand you have.
If discarding a card, the player would get a new card from the dealer. During this phase, all players should always have 5 cards.


#### Trick-Taking

During this round, each player in order gets a chance to call CHICAGO, which is explained later.

------ NO CHICAGO ------

The trick-taking phase, is a 5 turn round where each player gets to place a card on the table. The player after the dealer starts, and plays a card.
Then, in order, every other player has to place a card of the same suit if they have it. If another places a card of the same suit but of a higher value, that player wins the round and takes control over the game, and places their next card first.
The player who has the highest card of the played suit of the last round when there are no more cards, wins that round and gets 5 points. Also, the player with the highest hand also get the points for that hand.

------ SOMEONE CALLED CHICAGO! ------

The game is exactly like how it is explained with no chicago, however the player who called chicago will start. If, at any time, another player plays a card in the same suit with a higher value, it is an instant loss and the player who called chicago loses 15 points.
The rest of the players get no points during this round, even if they break the chicago.

If the player who called chicago never loses a turn and wins the last round, that player get 15 points and is able to win if they get 51 points.

#### Hand Points Table
+ Pair - 1 point
+ Two Pair - 2 points
+ Three-Of-A-Kind - 3 points
+ Straight - 4 points
+ Flush - 5 points
+ Full-House - 6 points
+ Four-Of-A-Kind - 7 points
+ Straight-Flush - 8 points

### The Project

This project was made with Unity as the game engine, as I'm pretty comfortable with it and with C# as the programming language.
As it is a multiplayer game I am also utilizing the Netcode For Gameobjects library for Unity, for networking logic.

I am also utilizing Steamworks SDK and Facepunch Library for transport, making it able to play online with friends through steam without needing to port-forward.

#### Cards

Every card in the game is a struct with a value and a Suit Enum, which then are then given to each player in a List<> in Player.cs script.

```
public enum Suit
{
    Heart,
    Diamond,
    Club,
    Spade,
    Joker
}
```

```
// Card.cs
public struct Card : INetworkSerializable
{
    [SerializeField]
    public int value;
    [SerializeField]
    public Suit _suit;

    public Card(int value, Suit suit)
    {
        this.value = value;
        _suit = suit;
    }

    public override string ToString()
    {
        return (value.ToString() + " of " + _suit.ToString());
    }

    public string FileName()
    {
        return (_suit.ToString() + value.ToString());
    }

}
```

The cards are managed in CardDeck.cs script and is done by generating 52 cards in a List<>, and then shuffling them.

```
public void InitializeDeck()
    {
        Cards.Clear();
        CachedThrownCards.Clear();

        for (int i = 1; i < 14; i++)
        {
            foreach (Suit suit in (Suit[])Enum.GetValues(typeof(Suit)))
            {
                if (suit == Suit.Joker) break;

                Card card = new Card(i, suit);

                Cards.Add(card);
            }
        }

        Cards.Shuffle();
    }
```

Each player then gets 5 card from a GetRandomCard() method in the same script, handled by the server/host.

```
public Card GetRandomCard()
    {
        if (Cards.Count <= 0)
        {
            return new Card(0, Suit.Joker);
        }

        int randomNum = UnityEngine.Random.Range(0, Cards.Count);

        Card randomCard = Cards[randomNum];

        if (randomNum >= 0 && randomNum < Cards.Count)
        {
            Cards.RemoveAt(randomNum);
        }

        return randomCard;
    }
```

#### Hand Detection

The hand detection logic is handled in HandDetector.cs.

This is being used in GameManager.cs to check each players hand and return a winner, as well as correct points for the hand.

```
// HandDetector.cs
public Hands CheckHand()
    {

        for (int i = 0; i < handCheckers.Count; i++)
        {
            if (handCheckers[i](hand))
            {
                return handRanks[i];
            }
        }

        return Hands.Nothing;
    }

    private bool CheckPair(List<Card> cards)
    {
        // See if there are 2 of the same valued cards
        return cards.GroupBy(card => card.value).Count(group => group.Count() == 2) == 1;
    }

    private bool CheckTwoPair(List<Card> cards)
    {
        // see if there are 2 lots of exactly 2 cards card the same rank.
        return cards.GroupBy(card => card.value).Count(group => group.Count() >= 2) == 2;
    }

    private bool CheckTrips(List<Card> cards)
    {
        // see if exactly 3 cards card the same rank.
        return cards.GroupBy(card => card.value).Any(group => group.Count() == 3);
    }

    private bool CheckFlush(List<Card> cards)
    {
        // see if 5 or more cards card the same rank.
        return cards.GroupBy(card => card._suit).Count(group => group.Count() >= 5) == 1;
    }

    private bool CheckFullHouse(List<Card> cards)
    {
        // check if trips and pair is true
        return CheckPair(cards) && CheckTrips(cards);
    }

    private bool CheckStraight(List<Card> cards)
    {
        return cards.GroupBy(card => card.value).Count() == cards.Count() && cards.Max(card => (int)card.value) - cards.Min(card => (int)card.value) == 4;
    }

    private bool CheckQuads(List<Card> cards)
    {
        // see if exactly 4 cards card the same rank.
        return cards.GroupBy(card => card.value).Any(group => group.Count() == 4);
    }

    // need to check same 5 cards
    private bool CheckStraightFlush(List<Card> cards)
    {
        // check if flush and straight are true.
        return CheckFlush(cards) && CheckStraight(cards);
    }

```

```
// GameManager.cs
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
            //yield return StartCoroutine(ShowAnnouncementText($"{currentWinner.profile.GetName()} has the highest {PokerHelper.HandName(highestHand)} and gets {GameRules.handPoints[highestHand]} points.", 3f));
            yield return StartCoroutine(ShowScoreText((int)highestHand, currentWinner, 3f));

            currentWinner.points.Value += GameRules.handPoints[highestHand];
        }
        yield return null;
    }
```

#### Game Loop

The whole game loop is in one script called GameManager.cs, using IEnumerators and Coroutines since I think they are a easy and clean way to write routines for a game like this.
The game loop is only running on the server/host and calls to methods on each players individual script with RPCs, when needed. For instance with UI, and setting active turn.

```
// GameManager.cs
private IEnumerator GameLoop()
    {
        ResetPlayers();

        // Actual game loop
        while (AllPlayersBelowMaxScore())
        {
            yield return StartCoroutine(DiscardPhase());
            yield return StartCoroutine(TrickTakingPhase());
            yield return StartCoroutine(EndRoundCleanup());
            yield return null;
        }

        // Win logic
        PlayerScript winner = GetChicagoWinner();

        while (winner == null)
        {
            winner = GetChicagoWinner();
            yield return null;
        }

        yield return ShowAnnouncementText($"{winner.profile.GetName()} WINS!!!", 5f);
        yield return ShowAnnouncementText($"Resetting Game...", 2f);

        background.ChangeToLobbyRpc();

        StartCoroutine(StartGame()); // Restart

    }
```

#### Players

The players are GameObjects with different scripts handling different logic.

+ Player.cs
    * Handles player logic, like discarding cards and playing cards.
+ PlayerUI.cs
    * Handles player UI, and is also connected to Player.cs for events such as button clicks.
+ PlayerProfile.cs
    * Handles other players profile, adding a UI when a player joins with the correct seatID, name, and profile picture.
    * The players are also handled by Events when players are joining Registering them and Unregistering them, keeping track of the players and seats.
+ ScoreScript.cs
    * Handles score, and some UI. This should probably have been in both Player.cs and PlayerUI.cs instead.

### Lobbies

For a Steam connection, I am using Lobbies from the library to create a lobby where players can join. This is handled in SteamManager.cs.

```
private async void GameLobbyJoinRequested(Lobby lobby, SteamId SteamID)
    {
        await lobby.Join();
    }

    private void LobbyEntered(Lobby lobby)
    {
        LobbySaver.instance.currentlobby = lobby;
        Debug.Log("Lobby entered!");
        //SceneManager.LoadScene("ChicagoLobby");
        if (NetworkManager.Singleton.IsHost) return;

        NetworkManager.Singleton.gameObject.GetComponent<FacepunchTransport>().targetSteamId = lobby.Owner.Id;
        NetworkManager.Singleton.StartClient();

        //StartGameServer();
    }

    private void LobbyCreated(Result result, Lobby lobby)
    {
        if (result == Result.OK)
        {
            lobby.SetPublic();
            lobby.SetJoinable(true);
            NetworkManager.Singleton.StartHost();
        }
    }

    public async void HostLobby()
    {
        await SteamMatchmaking.CreateLobbyAsync(4);
    }

    public void StartGameServer()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("ChicagoLobby", LoadSceneMode.Single);
        }

    }
```

#### UI

The UI was drawn by me in a pixel-art program called Piskel.

#### Audio

The audio for the game is handled through AudioManager.cs which is a singleton with RPC methods, to easily replicate and sync audio to each player.

#### General Thoughts & Working with Network Coding

Making a multiplayer game was a great challenge, and for easability and safety, I tried to keep as much game logic as possible on the server/host. This is to ensure consistency for all the players as well as preventing potential exploits from clients.
For instance, one challenge I had was card duplication and desync, as players would get cards. The players would be able to have two of the exact same card. This was because of trying to replicate the deck of cards, but making the GetRandomCards() method be client-sided. I solved this by making the having all the deck logic and getting cards logic be completely server sided, and notifying the players with RPCs when they get cards to update UI.

Every players UI is completely client-side as every player has completely different UIs, and the players are subscribing to network events and variables, with delegates or events, to change the UI when needed. For instance, points. This was actually really tricky as I had to be clever about how players profile would appear, and ensuring the turn order is correct. I also decided that the first player would be opposite the host, so if there are only two players it would look nicer. I solved this by assigning seat ids when players would join, and adding a player ui with a relative seat calculation.

```
// PlayerProfile.cs
int relativeSeat = (theirSeat - mySeat + seatContainers.Length) % seatContainers.Length;
```
The turn order would also have to be based on seat ids.
```
// GameManager.cs
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
```

```
// GameManager.cs
 // Get dealer's seat
        int dealerSeat = players[dealerPlayerIndex].profile.SeatId.Value;

        // Find who is next after dealer in turn order
        int startingIndex = turnOrder.FindIndex(p => p.profile.SeatId.Value == dealerSeat);
        startingIndex = (startingIndex + 1) % turnOrder.Count;
```

What helped a lot was being able to play locally on a single machine, easily testing different scenarios and edge-cases.

What was great with this project was that I could exercise my netcoding skills, as I both like working with Multiplayer games as well as I'm interning at a company where I am working with a multiplayer game.

I would like to continue working on this at some point, and improving a lot of stuff such as UI and Audio, and actually making finished game with this! The hand detector could also be improved as it doesn't really account for ace as 1 or 14 in a straight. AI and player statistics is also something I though of adding. As well as a quick play, searching randomly for a game.
