using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class CardDeck : NetworkBehaviour
{
    [SerializeField]
    private List<Card> Cards;

    //public NetworkList<Card> cardList;

    [SerializeField]
    private List<Card> CachedThrownCards;

    public static CardDeck instance;

    // Start is called before the first frame update
    void Start()
    {
        //if (!IsServer) return;

        instance = this;

        InitializeDeck();
    }

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

    //Returns and removes a random card from the deck
    public Card GetRandomCard()
    {
        if (Cards.Count <= 0)
        {
            return new Card(0, Suit.Joker);
        }

        int randomNum = UnityEngine.Random.Range(0, Cards.Count);

        Card randomCard = Cards[randomNum];

        //Cards.RemoveAt(randomNum);
        //RemoveCardFromDeckServerRpc(randomNum);
        if (randomNum >= 0 && randomNum < Cards.Count)
        {
            Cards.RemoveAt(randomNum);
        }

        return randomCard;
    }

    /*[Rpc(SendTo.Server)]
    void RequestRandomCardServerRpc(NetworkObjectReference playerRef)
    {
        if (Cards.Count == 0) return;
        if (!IsServer) return;
        if (!playerRef.TryGet(out NetworkObject playerObj)) return;

        int index = UnityEngine.Random.Range(0, Cards.Count);
        Card drawnCard = Cards[index];
        Cards.RemoveAt(index);

        // Tell the client which card they got
        GiveCardToPlayerClientRpc(
        drawnCard,
        playerRef,
        new RpcParams
        {
            Send = new RpcSendParams
            {
                Target = new[] { playerObj }
            }
        }
    );
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void GiveCardToPlayerClientRpc(Card card, NetworkObjectReference playerRef, RpcParams rpcParams = default)
    {
        if (!playerRef.TryGet(out NetworkObject playerObj)) return;

        var playerScript = playerObj.GetComponent<PlayerScript>();
        playerScript.ReceiveCard(card); 
    }*/

    [Rpc(SendTo.Server)]
    void RemoveCardFromDeckServerRpc(int value)
    {
        RemoveCardFromDeckClientRpc(value);
    }

    [Rpc(SendTo.Everyone)]
    void RemoveCardFromDeckClientRpc(int value)
    {
        if (value >= 0 && value < Cards.Count)
        {
            Cards.RemoveAt(value);
        }
        
    }
}
