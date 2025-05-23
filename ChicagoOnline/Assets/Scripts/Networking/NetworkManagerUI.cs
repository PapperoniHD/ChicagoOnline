using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;
    [SerializeField] private Button steamBtn;

    private void Awake()
    {
        hostBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
            hostBtn.gameObject.SetActive(false);
            clientBtn.gameObject.SetActive(false);
        });

        clientBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
            hostBtn.gameObject.SetActive(false);
            clientBtn.gameObject.SetActive(false);
        });
    }

    private void Start()
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
        {
            RemoveButtons();
        }
        else
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnClientConnected(ulong obj)
    {
        RemoveButtons();
    }

    public void RemoveButtons()
    {
        hostBtn.gameObject.SetActive(false);
        clientBtn.gameObject.SetActive(false);
        steamBtn.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

}
