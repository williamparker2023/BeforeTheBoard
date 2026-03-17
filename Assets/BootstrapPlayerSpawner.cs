using UnityEngine;
using Unity.Netcode;

public class BootstrapPlayerSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        SpawnAllPlayers();
    }

    private void SpawnAllPlayers()
    {
        int index = 0;

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Vector3 spawnPos = Vector3.zero;

            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                spawnPos = spawnPoints[index % spawnPoints.Length].position;
            }

            GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            NetworkObject netObj = player.GetComponent<NetworkObject>();
            netObj.SpawnAsPlayerObject(clientId, true);

            index++;
        }
    }
}