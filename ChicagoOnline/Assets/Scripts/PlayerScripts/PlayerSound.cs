using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerSound : NetworkBehaviour
{
    public static PlayerSound instance;

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

}

