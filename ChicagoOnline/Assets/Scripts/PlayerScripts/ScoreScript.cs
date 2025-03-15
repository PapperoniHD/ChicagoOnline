using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class ScoreScript : MonoBehaviour
{
    private int points;

    public TextMeshProUGUI playerName_Text;
    public TextMeshProUGUI score_Text;


    public void InitializeScore(PlayerScript playerscript, int playerIndex)
    {
        playerscript.points.OnValueChanged += UpdateScore;
        playerName_Text.text = "Player " + (playerIndex + 1);
    }

    private void UpdateScore(int previousValue, int newValue)
    {
        score_Text.text = newValue.ToString();
    }

}
