using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Components;

[RequireComponent(typeof(NetworkTransform))]
public class WaveEnemy : NetworkBehaviour
{
    [SerializeField] bool meleeType = true; //false = projectile
    [SerializeField] int classType = 0; // 0 = dps (high damage/speed, low health), 1 = tank (low damage/speed, high health)

    [SerializeField] public NetworkVariable<float> enemyHealth = new NetworkVariable<float>(10.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("PlayerAttack"))
        {
            // Only server executes this code
            if (IsServer)
            {
                enemyHealth.Value -= collision.gameObject.GetComponent<ProjectileTest>().damage;
                Destroy(collision.gameObject);
                Debug.Log("Enemy hit! Current health: " + enemyHealth.Value);

                if (enemyHealth.Value <= 0)
                {
                    KillEnemy();
                    // Destroy(gameObject);
                }
            }
        }
    }

    void KillEnemy()
    {
        if(IsServer)
        {
            Destroy(gameObject);
        }
        else
        {
            RequestScoreServerRpc();
        }
    }

    [ServerRpc]
    private void RequestScoreServerRpc(ServerRpcParams rpcParams = default)
    {
        // Only server executes this code
        Destroy(gameObject);
    }
}
