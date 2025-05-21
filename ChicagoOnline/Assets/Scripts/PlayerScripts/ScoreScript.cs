using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using Unity.Netcode;

public class ScoreScript : MonoBehaviour
{
    public int seatId;
    public PlayerProfile targetProfile;

    public TextMeshProUGUI playerName_Text;
    public TextMeshProUGUI score_Text;

    public Transform cardPlacement;
    public GameObject dealerButton;

    private void Start()
    {
    }


    public void InitializeScore(PlayerScript playerscript, int playerIndex, int seatId)
    {
        this.seatId = seatId;
        playerscript.points.OnValueChanged += UpdateScore;
        playerscript.isDealer.OnValueChanged += UpdateDealer;
        playerName_Text.text = "Seat " + (playerIndex);
        
    }

    private void UpdateDealer(bool previousValue, bool newValue)
    {
        dealerButton.SetActive(newValue);
    }

    private void UpdateScore(int previousValue, int newValue)
    {
        score_Text.text = newValue.ToString();
    }


}
