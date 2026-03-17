using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;

public class BootstrapPlayerSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private bool hasSpawnedPlayers = false;

    public override void OnNetworkSpawn()
    {
        Debug.Log($"BootstrapPlayerSpawner OnNetworkSpawn | IsServer={IsServer} | IsClient={IsClient}");

        if (!IsServer) return;

        StartCoroutine(SpawnPlayersAfterDelay());
    }

    private IEnumerator SpawnPlayersAfterDelay()
    {
        yield return new WaitForSeconds(1.0f);

        if (hasSpawnedPlayers) yield break;
        hasSpawnedPlayers = true;

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