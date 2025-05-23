using Steamworks.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbySaver : MonoBehaviour
{
    public Lobby currentlobby;
    public static LobbySaver instance;

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this.gameObject);
    }
}
