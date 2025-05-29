using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using Unity.Netcode;
using UnityEngine.UI;
using Steamworks;
using Steamworks.Data;
using System.Threading.Tasks;

public class ScoreScript : MonoBehaviour
{
    public int seatId;
    public PlayerProfile targetProfile;

    public TextMeshProUGUI playerName_Text;
    public TextMeshProUGUI score_Text;

    public Transform cardPlacement;
    public GameObject dealerButton;
    public RawImage avatarImage;
    public GameObject[] cards_UI;
    public TextMeshProUGUI chicagoText;

    public void InitializeScore(PlayerScript playerscript, int playerIndex, int seatId)
    {
        this.seatId = seatId;
        playerscript.points.OnValueChanged += UpdateScore;
        playerscript.isDealer.OnValueChanged += UpdateDealer;
        playerscript.handAmount.OnValueChanged += UpdateCardsUI;
        playerscript.chicagosWon.OnValueChanged += UpdateChicagoText;
        targetProfile = playerscript.profile;

        // Still set name before changing to steam info, in case steam info fails.
        playerName_Text.text = "Seat " + (playerIndex);

        if (SteamClient.IsValid)
        {
            StartCoroutine(AwaitSteamData());
        }         
    }

    private IEnumerator AwaitSteamData()
    {
        while (targetProfile != null && (targetProfile.steamId.Value == 0 || string.IsNullOrEmpty(targetProfile.steamName.Value.ToString())))
        {
            yield return null;
        }
        SetSteamProfile(targetProfile.steamId.Value, targetProfile.steamName.Value.ToString());
    }

    async void SetSteamProfile(ulong steamId, string steamName)
    {
        playerName_Text.text = steamName;
        var img = await SteamFriends.GetLargeAvatarAsync(steamId);
        if (img.HasValue)
        {
            avatarImage.enabled = true;
            avatarImage.texture = GetTextureFromImage(img.Value);
        }
        else
        {
            avatarImage.enabled = false;
            Debug.Log($"Failed to get avatar");
        }
        
    }

    public static Texture2D GetTextureFromImage(Steamworks.Data.Image image)
    {
        Texture2D texture = new Texture2D((int)image.Width, (int)image.Height);

        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                var p = image.GetPixel(x,y);
                texture.SetPixel(x,(int)image.Height - y, new UnityEngine.Color(p.r / 255f, p.g / 255f, p.b / 255f, p.a / 255f));
            }
        }
        texture.Apply();
        return texture;
    }


    private void UpdateDealer(bool previousValue, bool newValue)
    {
        dealerButton.SetActive(newValue);
    }

    private void UpdateScore(int previousValue, int newValue)
    {
        score_Text.text = newValue.ToString();
    }

    private void UpdateCardsUI(int previousValue, int newValue)
    {
        for (int i = 0; i < cards_UI.Length; i++)
        {
            if (i > newValue - 1)
            {
                cards_UI[i].SetActive(false);
            }
            else
            {
                cards_UI[i].SetActive(true);
            }
        }
    }

    private void UpdateChicagoText(int previousValue, int newValue)
    {
        chicagoText.SetText($"Chicagos: {newValue}");
    }
}
