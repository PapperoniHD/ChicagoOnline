using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerUI : NetworkBehaviour
{
    public Camera playerCamera;

    [SerializeField] private TextMeshProUGUI currentTurnText;
    [SerializeField] private TextMeshProUGUI explosiveText;

    [SerializeField] private GameObject canvas;
    [SerializeField] private Transform scoreboard;
    [SerializeField] private GameObject playerScorePrefab;

    [SerializeField]
    private List<ScoreScript> scoreList;

    public RectTransform[] seatContainers;

    void Start()
    {
        if (IsOwner)
        {
            GameManager.GM.currentTurn.OnValueChanged += UpdateTurnText;
        }
        else
        {
            canvas.gameObject.SetActive(false);
            playerCamera.enabled = false;
        }
        
    }

    private void UpdateTurnText(int previousValue, int newValue)
    {
        currentTurnText.text = "Current Turn: " + newValue.ToString();
    }

    [Rpc(SendTo.Owner)]
    public void ExplosiveTextRpc(string text)
    {
        explosiveText.SetText(text);
        explosiveText.GetComponent<Animator>().Play("Explode", -1, 0f);
    }

    [Rpc(SendTo.Owner)]
    public void AnnouncementTextRpc(string text)
    {
        explosiveText.SetText(text);
        explosiveText.GetComponent<Animator>().Play("ShowText", -1, 0f);
    }

    [Rpc(SendTo.Owner)]
    public void HideTextRpc()
    {
        explosiveText.GetComponent<Animator>().Play("HideText", -1, 0f);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void AddPlayerScoreObjectClientRpc(ulong playerNetworkObjectId, int playerIndex)
    {
        // Find the NetworkObject by ID
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkObjectId, out NetworkObject networkObject))
        {
            // Get the PlayerScript from the found NetworkObject
            PlayerScript playerScript = networkObject.GetComponent<PlayerScript>();
            if (playerScript != null)
            {
                GameObject scoreObj = Instantiate(playerScorePrefab, scoreboard);
                ScoreScript score = scoreObj.GetComponent<ScoreScript>();

                //score.InitializeScore(playerScript, playerIndex);

                scoreList.Add(score);
            }
        }
   
        

    }
}
