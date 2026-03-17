using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class LobbyPlayerManager : NetworkBehaviour
{
    public static LobbyPlayerManager Instance;

    public NetworkList<PlayerData> players;

    private void Awake()
    {
        Instance = this;
        players = new NetworkList<PlayerData>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        SubmitLocalName();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        bool alreadyExists = false;
        foreach (var p in players)
        {
            if (p.clientId == clientId)
            {
                alreadyExists = true;
                break;
            }
        }

        if (!alreadyExists)
        {
            players.Add(new PlayerData
            {
                clientId = clientId,
                playerName = "Player"
            });
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        for (int i = players.Count - 1; i >= 0; i--)
        {
            if (players[i].clientId == clientId)
            {
                players.RemoveAt(i);
            }
        }
    }

    private void SubmitLocalName()
    {
        if (ConnectionManager.Instance == null) return;

        string chosenName = ConnectionManager.Instance.PlayerName;
        SubmitNameRpc(chosenName);
    }

    [Rpc(SendTo.Server)]
    public void SubmitNameRpc(string name, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        for (int i = players.Count - 1; i >= 0; i--)
        {
            if (players[i].clientId == clientId)
            {
                players.RemoveAt(i);
            }
        }

        players.Add(new PlayerData
        {
            clientId = clientId,
            playerName = string.IsNullOrWhiteSpace(name) ? "Player" : name.Trim()
        });
    }
}