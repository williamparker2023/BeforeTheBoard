using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

[RequireComponent(typeof(NetworkTransform))]
public class EnemyMeleeCode : NetworkBehaviour
{
    [SerializeField] public float damage = 0.5f;

    [SerializeField] float LIFETIME = 0.1f;
    // private Vector3 mousePos;
    private Rigidbody2D rb;
    public float force;

     public void Initialize(float dmg)
    {
        damage = dmg;
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(!IsOwner) return;
        
        if (IsClient)
        {
            RequestDestroyServerRpc();
        }
        else
        {
            Destroy(gameObject, LIFETIME);
        }
    }

    // void OnTriggerEnter2D(Collider2D collision)
    // {
    //     if (!IsServer) return;

    //     /*
    //     if (collision.gameObject.CompareTag("PlayerAttack"))
    //     {
    //         Destroy(collision.gameObject);
    //         Destroy(gameObject);
    //     }
    //     */
    // }

    [ServerRpc]
    private void RequestDestroyServerRpc(ServerRpcParams rpcParams = default)
    {
        // Only server executes this code
        Destroy(gameObject, LIFETIME);
    }

    public override void OnNetworkDespawn()
    {
        gameObject.SetActive(false);
        base.OnNetworkDespawn();
    }
}
