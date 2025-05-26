using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerSound : NetworkBehaviour
{
    public static PlayerSound instance;

    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip[] hoverCardClips;
    [SerializeField] private AudioClip[] placeCardClips;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            instance = this;
        }
        else
        {
            this.gameObject.SetActive(false);
        }
    }

    public void PlayHoverCard()
    {
        int random = Random.Range(0, hoverCardClips.Length);

        source.PlayOneShot(hoverCardClips[random]);
    }

    public void PlayPlaceCard()
    {
        int random = Random.Range(0, placeCardClips.Length);

        source.PlayOneShot(placeCardClips[random]);
    }
}

