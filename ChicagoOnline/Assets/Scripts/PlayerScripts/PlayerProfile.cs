﻿using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerProfile : NetworkBehaviour
{
    // For steam
    public NetworkVariable<ulong> steamId = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString128Bytes> steamName = new(writePerm: NetworkVariableWritePermission.Server);
    public Texture2D profilePicture; 

    // Events for registry
    public static event Action<PlayerProfile> OnProfileSpawned;
    public static event Action<PlayerProfile> OnProfileDespawned;

    // Networked seat ID 
    public NetworkVariable<int> SeatId = new(-1);

    [Header("UI")]
    public GameObject seatsCanvas;
    public RectTransform[] seatContainers;
    public GameObject profileUIPrefab;

    // UI instances, keyed by ClientID
    private Dictionary<ulong, RectTransform> spawnedUI = new Dictionary<ulong, RectTransform>();
    [SerializeField] Transform myCardPlacement;

    private bool _uiInitialized = false;
    public override void OnNetworkSpawn()
    {
        OnProfileSpawned?.Invoke(this);

        if (IsServer)
        {
            SeatId.Value = GameManager.GM.AssignSeatId();
        }
            
        if (!IsOwner)
        {
            seatsCanvas.SetActive(false);
            
        }
        else
        {
            GameUI.Instance.RegisterPlayer(this);
            SeatId.OnValueChanged += OnSeatIdAssigned;

            OnSeatIdAssigned(-1, SeatId.Value);
        }

        if (IsOwner && SteamClient.IsValid)
        {
            SubmitSteamDataServerRpc(SteamClient.SteamId, SteamClient.Name);
        }

    }

    public override void OnNetworkDespawn()
    {
        OnProfileDespawned?.Invoke(this);

        GameUI.Instance.UnregisterPlayer(this);
        SeatId.OnValueChanged -= OnSeatIdAssigned;
    }

    [Rpc(SendTo.Server)]
    private void SubmitSteamDataServerRpc(ulong id, string name)
    {
        steamId.Value = id;
        steamName.Value = new FixedString128Bytes(name);
    }

    private void OnSeatIdAssigned(int previousValue, int newValue)
    {
        if (_uiInitialized) return;
        _uiInitialized = true;

        Debug.Log($"[PlayerProfile] Seat assigned: {newValue}");

        GameUI.Instance.OnPlayerJoined += TryAddUI;
        GameUI.Instance.OnPlayerLeft += TryRemoveUI;

        StartCoroutine(DelayedUIInit());
    }


    private IEnumerator DelayedUIInit()
    {
        var netMgr = NetworkManager.Singleton;

        // Wait one frame to ensure all PlayerProfiles are spawned
        yield return null;

        // Wait until we have all profiles
        while (GameUI.Instance.AllProfiles.Count < netMgr.ConnectedClientsIds.Count)
            yield return null;

        // Wait until all other profiles have valid seat IDs
        while (GameUI.Instance.AllProfiles
            .Where(p => p != this)
            .Any(p => p.SeatId.Value < 0 || p.SeatId.Value >= seatContainers.Length))
        {
            yield return null;
        }

        foreach (var other in GameUI.Instance.AllProfiles)
        {
            TryAddUI(other);
        }

        TryUpdateLayout();
    }
    private void TryAddUI(PlayerProfile p)
    {
        if (!IsOwner || p == this) return;
        if (spawnedUI.ContainsKey(p.OwnerClientId)) return;

        if (p.SeatId.Value < 0 || p.SeatId.Value >= seatContainers.Length)
        {
            StartCoroutine(WaitForSeatIdThenAddUI(p));
            return;
        }

        Debug.Log($"[TryAddUI] Creating UI for client {p.OwnerClientId}");

        var rect = Instantiate(profileUIPrefab, seatsCanvas.transform).GetComponent<RectTransform>();
        spawnedUI[p.OwnerClientId] = rect;

        var scoreScript = rect.GetComponent<ScoreScript>();
        var playerScript = p.GetComponent<PlayerScript>();
        int playerIndex = GameUI.Instance.AllProfiles.IndexOf(p);
        scoreScript.InitializeScore(playerScript, playerIndex, p.SeatId.Value);

        TryUpdateLayout();
    }

    private IEnumerator WaitForSeatIdThenAddUI(PlayerProfile p)
    {
        while (p.SeatId.Value < 0 || p.SeatId.Value >= seatContainers.Length)
            yield return null;

        TryAddUI(p);
    }

    private void TryRemoveUI(PlayerProfile p)
    {
        if (!IsOwner) return;
        if (spawnedUI.TryGetValue(p.OwnerClientId, out var rect))
        {
            Destroy(rect.gameObject);
            spawnedUI.Remove(p.OwnerClientId);
            UpdateLayout();
        }
    }

    private void TryUpdateLayout()
    {
        if (GameUI.Instance.AllProfiles
            .Where(p => p != this)
            .All(p => spawnedUI.ContainsKey(p.OwnerClientId)))
        {
            UpdateLayout();
        }
    }
    private void UpdateLayout()
    {
        int mySeat = SeatId.Value;

        foreach (var kvp in spawnedUI)
        {
            ulong otherClientId = kvp.Key;
            RectTransform rect = kvp.Value;
            ScoreScript score = kvp.Value.GetComponent<ScoreScript>();


            PlayerProfile other = GameUI.Instance.AllProfiles.FirstOrDefault(p => p.OwnerClientId == otherClientId);
            if (other == null)
            {
                Debug.LogWarning($"[UpdateLayout] Could not find PlayerProfile for client {otherClientId}");
                continue;
            }

            int theirSeat = other.SeatId.Value;
            Debug.Log($"[UpdateLayout] Player {other.OwnerClientId} has seat {theirSeat}");

            int relativeSeat = (theirSeat - mySeat + seatContainers.Length) % seatContainers.Length;

            rect.SetParent(seatContainers[relativeSeat], false);
            rect.localPosition = Vector3.zero;

            if (score != null)
            {
                CardTablePlacement placement = seatContainers[relativeSeat].GetComponent<CardTablePlacement>();
                if (placement == null) return;
                score.cardPlacement = placement.cardPlacement;

            }
            Debug.Log($"Me: {mySeat}, Them: {theirSeat}, RelativeSeat: {relativeSeat}");
        }
    }

    public Transform GetCardSpawn(int seatId)
    {
        if (seatId == this.SeatId.Value)
        {
            return myCardPlacement;
        }

        foreach (var kvp in spawnedUI)
        {
            ulong otherClientId = kvp.Key;
            PlayerProfile other = GameUI.Instance.AllProfiles.FirstOrDefault(p => p.OwnerClientId == otherClientId);
            if (other == null)
            {
                Debug.LogWarning($"[UpdateLayout] Could not find PlayerProfile for client {otherClientId}");
                continue;
            }

            if (other.SeatId.Value == seatId)
            {
                ScoreScript score = kvp.Value.GetComponent<ScoreScript>();
                if (score != null)
                {
                    return score.cardPlacement;
                }
                
            }
        }
        return null;
    }

    public string GetName()
    {
        if (SteamClient.IsValid)
        {
            return steamName.Value.ToString();
        }
        else
        {
            return $"Seat {SeatId.Value}";
        }
    }


    async void SetSteamProfilePicture()
    {
        var img = await SteamFriends.GetLargeAvatarAsync(steamId.Value);
        if (img.HasValue)
        {
            profilePicture = SteamHelper.GetTextureFromImage(img.Value);
        }
    }

    public Texture2D GetProfilePicture()
    {
        if (SteamClient.IsValid)
        {
            if (profilePicture != null)
            {
                return profilePicture;
            }
            else
            {
                SetSteamProfilePicture();
                return profilePicture;
            }
        }
        else
        {
            return null;
        }
        
    }

}




