using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

[RequireComponent(typeof(NetworkTransform))]
public class PlayerMove : NetworkBehaviour
{
    [SerializeField] private float speed = 6f;

    void Update()
    {
        if (!IsOwner) return;

        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        Vector3 dir = new Vector3(x, y, 0f).normalized;

        transform.position += dir * speed * Time.deltaTime;
    }
}