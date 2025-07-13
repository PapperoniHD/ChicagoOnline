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
    [SerializeField] private Button quit;
    [SerializeField] private Camera menuCamera;

    private void Awake()
    {
        hostBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
            hostBtn.gameObject.SetActive(false);
            clientBtn.gameObject.SetActive(false);
            RemoveButtons();

        });

        clientBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
            hostBtn.gameObject.SetActive(false);
            clientBtn.gameObject.SetActive(false);
            RemoveButtons();
        });

        quit.onClick.AddListener(() =>
        {
            Application.Quit();
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
        this.transform.parent.gameObject.SetActive(false);

        if (menuCamera != null)
        {
            Destroy(menuCamera.gameObject);
        }
        
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

}
