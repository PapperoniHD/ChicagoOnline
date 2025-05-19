using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BackgroundManager : NetworkBehaviour
{
    public Material targetMaterial;

    public Color LobbyColor;
    public Color CollectingColor;
    public Color TrickTakingColor;

    public float transitionDuration = 1f;

    public void Start()
    {
        targetMaterial.SetColor("_Color", LobbyColor);
    }

    [Rpc(SendTo.Everyone)]
    public void ChangeToTrickTakingRpc()
    {
        if (targetMaterial == null) return;
        StopAllCoroutines();
        StartCoroutine(LerpColor(TrickTakingColor));
    }
    [Rpc(SendTo.Everyone)]
    public void ChangeToDiscardRpc()
    {
        if (targetMaterial == null) return;
        StopAllCoroutines();
        StartCoroutine(LerpColor(CollectingColor));
    }

    [Rpc(SendTo.Everyone)]
    public void ChangeToLobbyRpc()
    {
        if (targetMaterial == null) return;
        StopAllCoroutines();
        StartCoroutine(LerpColor(TrickTakingColor));
    }

    IEnumerator LerpColor(Color color)
    {
        Color startColor = targetMaterial.GetColor("_Color");
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            Color lerped = Color.Lerp(startColor, color, t);
            targetMaterial.SetColor("_Color", lerped);
            yield return null;
        }

        targetMaterial.SetColor("_Color", color);
    }

}
