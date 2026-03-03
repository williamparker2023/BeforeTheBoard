using Unity.Netcode;
using UnityEngine;

public class PlayerSpawnOffset : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // Spread players out horizontally by clientId
        float x = (int)OwnerClientId * 2.5f;
        transform.position = new Vector3(x, 0f, 0f);
    }
}