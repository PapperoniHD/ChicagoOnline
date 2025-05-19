using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using Unity.Netcode;

public class ScoreScript : MonoBehaviour
{
    public NetworkVariable<int> SeatId = new();
    public PlayerProfile targetProfile;

    public TextMeshProUGUI playerName_Text;
    public TextMeshProUGUI score_Text;

    private void Start()
    {
    }


    public void InitializeScore(PlayerScript playerscript, int playerIndex)
    {
        playerscript.points.OnValueChanged += UpdateScore;
        playerName_Text.text = "Seat " + (playerIndex);
    }

    private void UpdateScore(int previousValue, int newValue)
    {
        score_Text.text = newValue.ToString();
    }


}
