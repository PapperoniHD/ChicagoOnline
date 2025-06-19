using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : NetworkBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public PlayerScript playerScript;

    [Header("Buttons")]
    public Button discardCardButton;
    public Button sortButton;
    public Button endTurnButton;
    public Button startButton;

    [Header("Cards")]
    public GameObject cardPrefab;
    public RectTransform cardPos;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI currentTurnText;
    [SerializeField] private TextMeshProUGUI explosiveText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private RawImage scoreProfilePicture;
    [SerializeField] private TextMeshProUGUI scoreProfileName;

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
        SetupButtons();

    }

    private void SetupButtons()
    {
        if (!IsLocalPlayer) return;

        discardCardButton.onClick.AddListener(playerScript.DiscardCards);
        sortButton.onClick.AddListener(playerScript.Check);
        endTurnButton.onClick.AddListener(playerScript.EndTurn);

        discardCardButton.gameObject.SetActive(false);
        endTurnButton.gameObject.SetActive(false);


        if (IsServer)
        {
            startButton.gameObject.SetActive(true);
            startButton.onClick.AddListener(playerScript.StartGame);
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
    public void ScoreTextRpc(int hand, NetworkObjectReference Player)
    {
        
        if (Player.TryGet(out NetworkObject netObj))
        {
            PlayerProfile profile = netObj.GetComponent<PlayerProfile>();
            if (profile == null)
            {
                Debug.LogError("[PlayerUI] PlayerProfile not found!");
                return;
            }

            waitingUI.SetActive(false);
            scoreText.SetText($"{PokerHelper.HandName((Hands)hand)} +{GameRules.handPoints[(Hands)hand]}");

            scoreProfilePicture.texture = profile.GetProfilePicture();
            scoreProfileName.text = profile.GetName();

            scoreText.GetComponent<Animator>().Play("ShowText", -1, 0f);
        }
    }

    [Rpc(SendTo.Owner)]
    public void HideScoreTextRpc()
    {
        scoreText.GetComponent<Animator>().Play("HideText", -1, 0f);
    }

    [Rpc(SendTo.Owner)]
    public void HideAnnouncementTextRpc()
    {
        explosiveText.GetComponent<Animator>().Play("HideText", -1, 0f);
    }

}
