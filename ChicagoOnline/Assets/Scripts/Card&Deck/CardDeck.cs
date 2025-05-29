using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class CardDeck : NetworkBehaviour
{
    [SerializeField] private List<Card> Cards;
    [SerializeField] private List<Card> CachedThrownCards;

    public static CardDeck instance;

    void Start()
    {
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

        if (randomNum >= 0 && randomNum < Cards.Count)
        {
            Cards.RemoveAt(randomNum);
        }

        return randomCard;
    }
}
