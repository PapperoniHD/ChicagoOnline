using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PlayerProfile : NetworkBehaviour
{
    // events for registry
    public static event Action<PlayerProfile> OnProfileSpawned;
    public static event Action<PlayerProfile> OnProfileDespawned;

    // --- networked seat ID ---
    //public NetworkVariable<int> SeatId = new(-1);
    public NetworkVariable<int> SeatId = new(-1);

    [Header("UI (prefab‐child)")]
    [Tooltip("Disable this canvas on non‐owner")]
    public GameObject seatsCanvas;
    [Tooltip("0 = self(bottom), 1 = right, 2 = top, 3 = left")]
    public RectTransform[] seatContainers;
    [Tooltip("Small UI prefab showing avatar/profile (Image+Name)")]
    public GameObject profileUIPrefab;

    // track spawned UI instances (keyed by clientId)
    private Dictionary<ulong, RectTransform> spawnedUI = new Dictionary<ulong, RectTransform>();
    [SerializeField] Transform myCardPlacement;

    private bool _uiInitialized = false;
    public override void OnNetworkSpawn()
    {
        OnProfileSpawned?.Invoke(this);

        if (IsServer)
            SeatId.Value = GameManager.GM.AssignSeatId(OwnerClientId);

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

        
    }

    public override void OnNetworkDespawn()
    {
        OnProfileDespawned?.Invoke(this);

        GameUI.Instance.UnregisterPlayer(this);
        SeatId.OnValueChanged -= OnSeatIdAssigned;
    }

    private void OnSeatIdAssigned(int previousValue, int newValue)
    {
        if (_uiInitialized) return;
        _uiInitialized = true;

        Debug.Log($"[PlayerProfile] Seat assigned: {newValue}");

        GameUI.Instance.OnPlayerJoined += TryAddUI;
        GameUI.Instance.OnPlayerLeft += TryRemoveUI;

        // Seed UI for existing players
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
        // Only re-layout if we have UI entries for all other players
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

}




