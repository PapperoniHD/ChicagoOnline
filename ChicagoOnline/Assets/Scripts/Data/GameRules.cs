using System.Collections;
using System.Collections.Generic;

public static class GameRules
{
    public static int maxHandAmount = 5;
    public static int pointsToWin = 52;
    public static int turnCount = 3;
    public static int winOnTwo_Points = 10;
    public static int roundWin_Points = 5;
    public static int ChicagoPoints = 15;

    public static readonly Dictionary<Hands, int> handPoints = new Dictionary<Hands, int>
    {
        { Hands.Nothing, 0 },
        { Hands.Pair, 1 },
        { Hands.Two_Pair, 2 },
        { Hands.Three_Of_A_Kind, 3 },
        { Hands.Straight, 4 },
        { Hands.Flush, 5 },
        { Hands.Full_House, 6 },
        { Hands.Four_Of_A_Kind, 7 },
        { Hands.Straight_Flush, 8 },
        { Hands.Royal_Flush, 52 },

    };
}
