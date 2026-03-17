using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine.SceneManagement;

public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager Instance;

    [SerializeField] private string joinCodeString;

    public string JoinCode => joinCodeString;
    [SerializeField] private string playerName = "Player";

    public string PlayerName => playerName;

    public void SetPlayerName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            playerName = "Player";
        else
            playerName = newName.Trim();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client connected: {clientId}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client disconnected: {clientId}");
    }

    public async void StartHost()
    {
        const int maxPlayers = 4;
        const string connectionType = "dtls";

        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        joinCodeString = joinCode;

        Debug.Log("Join Code: " + joinCode);

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));

        bool ok = NetworkManager.Singleton.StartHost();
        Debug.Log("StartHost() => " + ok);

        if (ok)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("Host failed to start.");
        }
    }

    public async void StartClient(string joinCode)
    {
        const string connectionType = "dtls";

        joinCodeString = joinCode;

        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));

        bool ok = NetworkManager.Singleton.StartClient();
        Debug.Log("StartClient() => " + ok);

        if (!ok)
        {
            Debug.LogError("Client failed to start.");
        }
        else
        {
            Debug.LogError("Client failed to start.");
        }
    }

    public void StartGame()
    {
        if (!NetworkManager.Singleton.IsHost)
            return;

        NetworkManager.Singleton.SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
    }

    public void LeaveLobby()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        joinCodeString = "";
        SceneManager.LoadScene("MainMenu");
    }
}