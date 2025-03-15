using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HandDetector : MonoBehaviour
{
    List<Card> hand;

    private readonly List<Func<List<Card>, bool>> handCheckers;
    private readonly List<Hands> handRanks;

    public HandDetector(List<Card> hand)
    {
        this.hand = hand;
        //Array.Sort(this.hand, (x, y) => x.value.CompareTo(y.value));     

        handCheckers = new List<Func<List<Card>, bool>>
        {
            CheckStraightFlush,
            CheckQuads,
            CheckFullHouse,
            CheckFlush,
            CheckStraight,
            CheckTrips,
            CheckTwoPair,
            CheckPair
        };

        handRanks = new List<Hands>
        {
            Hands.Straight_Flush,
            Hands.Four_Of_A_Kind,
            Hands.Full_House,
            Hands.Flush,
            Hands.Straight,
            Hands.Three_Of_A_Kind,
            Hands.Two_Pair,
            Hands.Pair
        };

    }

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
        //See if there are 2 of the same valued cards
        return cards.GroupBy(card => card.value).Count(group => group.Count() == 2) == 1;
    }

    private bool CheckTwoPair(List<Card> cards)
    {
        //see if there are 2 lots of exactly 2 cards card the same rank.
        return cards.GroupBy(card => card.value).Count(group => group.Count() >= 2) == 2;
    }

    private bool CheckTrips(List<Card> cards)
    {
        //see if exactly 3 cards card the same rank.
        return cards.GroupBy(card => card.value).Any(group => group.Count() == 3);
    }

    private bool CheckFlush(List<Card> cards)
    {
        //see if 5 or more cards card the same rank.
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
        //see if exactly 4 cards card the same rank.
        return cards.GroupBy(card => card.value).Any(group => group.Count() == 4);
    }

    // need to check same 5 cards
    private bool CheckStraightFlush(List<Card> cards)
    {
        // check if flush and straight are true.
        return CheckFlush(cards) && CheckStraight(cards);
    }

}
