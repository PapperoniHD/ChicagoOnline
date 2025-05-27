using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerUI : NetworkBehaviour
{
    public Camera playerCamera;
    public PlayerScript playerScript;

    [SerializeField] private TextMeshProUGUI currentTurnText;
    [SerializeField] private TextMeshProUGUI explosiveText;

    [SerializeField] private GameObject canvas;
    [SerializeField] private Transform scoreboard;
    [SerializeField] private GameObject playerScorePrefab;

    [SerializeField] private TextMeshProUGUI localScoreText;
    [SerializeField] private GameObject dealerButtonUI;
    [SerializeField] private GameObject chicagoPromptUI;
    [SerializeField] private GameObject waitingUI;
    public GameObject yourTurnUI;
    public GameObject chooseCardsUI;

    [SerializeField]
    private List<ScoreScript> scoreList;

    public RectTransform[] seatContainers;


    void Start()
    {
        if (IsOwner)
        {
            GameManager.GM.currentTurn.OnValueChanged += UpdateTurnText;
            playerScript.points.OnValueChanged += UpdateScore;
            playerScript.isDealer.OnValueChanged += UpdateDealer;

        }
        else
        {
            canvas.gameObject.SetActive(false);
            playerCamera.enabled = false;
        }
        
    }

    [Rpc(SendTo.Owner)]
    public void AskForChicagoRpc()
    {
        chicagoPromptUI.SetActive(true);
    }

    [Rpc(SendTo.Owner)]
    public void WaitingForChicagoUIRpc(bool waiting)
    {
        waitingUI.SetActive(waiting);
    }

    [Rpc(SendTo.Server)]
    public void SetChicagoResponeRpc(bool didCall)
    {
        playerScript.calledChicago.Value = didCall;
        playerScript.hasAnsweredChicago.Value = true;
        OnChicagoResponseClientRpc();
    }

    [Rpc(SendTo.Owner)]
    public void OnChicagoResponseClientRpc()
    {
        chicagoPromptUI.SetActive(false);
    }


    private void UpdateDealer(bool previousValue, bool newValue)
    {
        dealerButtonUI.SetActive(newValue);
    }

    private void UpdateScore(int previousValue, int newValue)
    {
        localScoreText.SetText(newValue.ToString());
    }

    private void UpdateTurnText(int previousValue, int newValue)
    {
        currentTurnText.text = "Current Turn: " + newValue.ToString();
    }

    [Rpc(SendTo.Owner)]
    public void ExplosiveTextRpc(string text)
    {
        waitingUI.SetActive(false);
        explosiveText.SetText(text);
        explosiveText.GetComponent<Animator>().Play("Explode", -1, 0f);
    }

    [Rpc(SendTo.Owner)]
    public void AnnouncementTextRpc(string text)
    {
        waitingUI.SetActive(false);
        explosiveText.SetText(text);
        explosiveText.GetComponent<Animator>().Play("ShowText", -1, 0f);
    }

    [Rpc(SendTo.Owner)]
    public void HideTextRpc()
    {
        explosiveText.GetComponent<Animator>().Play("HideText", -1, 0f);
    }

}
