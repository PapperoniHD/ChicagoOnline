using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class GameUI : NetworkBehaviour
{
    public static GameUI Instance;

    public PlayerProfile LocalProfile;
    public List<PlayerProfile> AllProfiles = new();
    public Dictionary<ulong, PlayerProfile> AllProfilesDict = new();

    public event Action<PlayerProfile> OnPlayerJoined;
    public event Action<PlayerProfile> OnPlayerLeft;

    public GameObject profileUIPrefab;

    void Awake()
    {
        Instance = this;
    }

    public void OnEnable()
    {
        PlayerProfile.OnProfileSpawned += RegisterPlayer;
        PlayerProfile.OnProfileDespawned += UnregisterPlayer;

    }

    public void OnDisable()
    {
        PlayerProfile.OnProfileSpawned -= RegisterPlayer;
        PlayerProfile.OnProfileDespawned -= UnregisterPlayer;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }

    public void RegisterPlayer(PlayerProfile player)
    {
        if (!AllProfiles.Contains(player))
        {
            Debug.Log($"[GameUI] Registering (seed/spawn) player {player.OwnerClientId}");
            AllProfiles.Add(player);
            //AllProfilesDict.Add(OwnerClientId, player);
            OnPlayerJoined?.Invoke(player);
        }

        if (player.IsOwner)
        {
            LocalProfile = player;
        }
    }

    

    public void UnregisterPlayer(PlayerProfile player)
    {
        if (AllProfiles.Remove(player))
        {
            Debug.Log($"[GameUI] Unregistering player {player.OwnerClientId}");
            OnPlayerLeft?.Invoke(player);
        }
        //AllProfilesDict.Remove(OwnerClientId);
    }

    public Transform GetCardParent(int seatId)
    {
        return LocalProfile.GetCardSpawn(seatId);
    }
}

