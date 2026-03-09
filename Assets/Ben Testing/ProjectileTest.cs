using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

[RequireComponent(typeof(NetworkTransform))]
public class ProjectileTest : NetworkBehaviour
{
    // [SerializeField] float damage = 0.5f;

    [SerializeField] float LIFETIME = 5.0f;
    // private Vector3 mousePos;
    private Rigidbody2D rb;
    private Vector3 mousePos;
    private Camera mainCam;
    public float force;

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

        mainCam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        rb = GetComponent<Rigidbody2D>();
        mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Vector3 direction = mousePos - transform.position;
        Vector3 rotation = transform.position - mousePos;

        rb.linearVelocity = new Vector2(direction.x, direction.y).normalized * force;
        float rot = Mathf.Atan2(rotation.y, rotation.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, rot + 90);
    }

    [ServerRpc]
    private void RequestDestroyServerRpc(ServerRpcParams rpcParams = default)
    {
        // Only server executes this code
        Destroy(gameObject, LIFETIME);
    }
}
