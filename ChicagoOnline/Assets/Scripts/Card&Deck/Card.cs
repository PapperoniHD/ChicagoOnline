using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum Suit
{
    Heart,
    Diamond,
    Club,
    Spade,
    Joker
}

[System.Serializable]
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

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        
    }
}
