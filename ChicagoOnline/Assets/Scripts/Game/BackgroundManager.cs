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
    public Color ChicagoColor;

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

    [Rpc(SendTo.Everyone)]
    public void ChangeToChicagoRpc()
    {
        if (targetMaterial == null) return;
        StopAllCoroutines();
        StartCoroutine(ChicagoBlink());
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

    IEnumerator ChicagoBlink()
    {
        Color startColor = CollectingColor;
        float elapsed = 0f;

        float blinkDuration = 0.2f;

        for (int i = 0; i < 5; i++)
        {
            while (elapsed < blinkDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;
                Color lerped = Color.Lerp(startColor, ChicagoColor, t);
                targetMaterial.SetColor("_Color", lerped);
                yield return null;
            }

            elapsed = 0f;
            targetMaterial.SetColor("_Color", ChicagoColor);
            yield return new WaitForSeconds(0.2f);
            
            while (elapsed < blinkDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;
                Color lerped = Color.Lerp(ChicagoColor, startColor, t);
                targetMaterial.SetColor("_Color", lerped);
                yield return null;
            }

            targetMaterial.SetColor("_Color", ChicagoColor);
            elapsed = 0;
        }

        elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            Color lerped = Color.Lerp(startColor, ChicagoColor, t);
            targetMaterial.SetColor("_Color", lerped);
            yield return null;
        }

        targetMaterial.SetColor("_Color", ChicagoColor);
    }

}
