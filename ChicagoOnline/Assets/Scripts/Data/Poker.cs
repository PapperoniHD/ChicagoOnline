public enum Hands
{
    Royal_Flush,
    Straight_Flush,
    Four_Of_A_Kind,
    Full_House,
    Flush,
    Straight,
    Three_Of_A_Kind,
    Two_Pair,
    Pair,
    Nothing
}

public static class PokerHelper
{
    public static string HandName(Hands hand)
    {
        string handName = hand.ToString().Replace("_", " ");
        return handName;
    }
}