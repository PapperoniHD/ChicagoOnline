using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AudioManager : NetworkBehaviour
{
    public static AudioManager instance;

    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip[] hoverCardClips;
    [SerializeField] private AudioClip[] selectCardClips;
    [SerializeField] private AudioClip[] placeCardClips;
    [SerializeField] private AudioClip getCardsClip;
    [SerializeField] private AudioClip buttonClip;
    [SerializeField] private AudioClip noChicago;
    [SerializeField] private AudioClip loseChicago;
    [SerializeField] private AudioClip winChicago;

    private float antiSpamTimer = 0.05f;

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        antiSpamTimer -= Time.deltaTime;
    }

    public void PlayHoverCard()
    {
        if (antiSpamTimer < 0)
        {
            int random = Random.Range(0, hoverCardClips.Length);

            source.PlayOneShot(hoverCardClips[random]);
            antiSpamTimer = 0.05f;
        }
    }

    public void PlaySelectCard()
    {
        int random = Random.Range(0, selectCardClips.Length);

        source.PlayOneShot(selectCardClips[random]);
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

    [Rpc(SendTo.Everyone)]
    public void PlayNoChicagoRpc()
    {
        source.PlayOneShot(noChicago);
    }

    [Rpc(SendTo.Everyone)]
    public void PlayWinChicagoRpc()
    {
        source.PlayOneShot(winChicago);
    }

    [Rpc(SendTo.Everyone)]
    public void PlayLoseChicagoRpc()
    {
        source.PlayOneShot(loseChicago);
    }
}
