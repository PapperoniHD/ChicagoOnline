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
    [SerializeField] private AudioClip getCardsClip;
    [SerializeField] private AudioClip buttonClip;
    [SerializeField] private AudioClip noChicago;
    [SerializeField] private AudioClip loseChicago;
    [SerializeField] private AudioClip winChicago;
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

    public void PlayButton()
    {
        source.PlayOneShot(buttonClip);
    }

    public void PlayGetCards()
    {
        source.PlayOneShot(getCardsClip);
    }

    public void PlayNoChicago()
    {
        source.PlayOneShot(noChicago);
    }

    public void PlayWinChicago()
    {
        source.PlayOneShot(winChicago);
    }

    public void PlayLoseChicago()
    {
        source.PlayOneShot(loseChicago);
    }

}

