using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

[RequireComponent(typeof(NetworkTransform))]
public class MeleeAttack : NetworkBehaviour
{
    [SerializeField] public float damage = 0.5f;
    [SerializeField] float LIFETIME = 0.1f;
    // private Vector3 mousePos;
    private Vector3 mousePos;
    private Camera mainCam;
    public float force;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {        
        if (IsClient && !IsServer)
        {
            RequestDestroyServerRpc();
        }
        else
        {
            Destroy(gameObject, LIFETIME);
        }

    }

    [ServerRpc]
    private void RequestDestroyServerRpc(ServerRpcParams rpcParams = default)
    {
        // Only server executes this code
        Destroy(gameObject, LIFETIME);
    }
}
