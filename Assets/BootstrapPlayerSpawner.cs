using UnityEngine;
using Unity.Netcode;

public class BootstrapPlayerSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    public override void OnNetworkSpawn()
    {
        Debug.Log($"BootstrapPlayerSpawner OnNetworkSpawn | IsServer={IsServer} | IsClient={IsClient}");

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

            Debug.Log($"Spawning player for client {clientId} at {spawnPos}");

            GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

            NetworkObject netObj = player.GetComponent<NetworkObject>();
            if (netObj == null)
            {
                Debug.LogError("Spawned player prefab has no NetworkObject!");
                return;
            }

            netObj.SpawnAsPlayerObject(clientId, true);

            Debug.Log($"Spawned player object: {player.name} | Owner={clientId}");

            index++;
        }
    }
}