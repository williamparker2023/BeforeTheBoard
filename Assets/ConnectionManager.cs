using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using TMPro;
using UnityEngine.UI;

public class ConnectionManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField joinCodeInput;
    public TMP_Text statusText;
    [SerializeField] GameObject serverUiObject;
    [SerializeField] TextMeshProUGUI serverCodeText;
    [SerializeField] string joinCodeString;

    void Awake()
    {
        // These callbacks are the best “proof” in the Editor console.
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client connected: {clientId}");

        // If you're a client and you connected to host, this will fire too.
        if (statusText != null)
            statusText.text = $"Connected! ClientId={clientId}";
            serverUiObject.SetActive(false);
            serverCodeText.text = $"{joinCodeString}";
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client disconnected: {clientId}");
        if (statusText != null)
            statusText.text = $"Disconnected. ClientId={clientId}";
            serverUiObject.SetActive(true);
            serverCodeText.text = $"";
    }

    public async void StartHost()
    {
        const int maxPlayers = 4;
        const string connectionType = "dtls";

        if (statusText != null) statusText.text = "Starting Host...";

        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        Debug.Log("Join Code: " + joinCode);
        if (statusText != null) {
            statusText.text = $"HOST\nCode: {joinCode}";
            joinCodeString = joinCode;
        }

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));

        bool ok = NetworkManager.Singleton.StartHost();
        Debug.Log("StartHost() => " + ok);
        if (!ok && statusText != null) statusText.text = "Host failed to start (see Console).";
    }

    public void JoinFromInput()
    {
        var code = joinCodeInput != null ? joinCodeInput.text.Trim() : "";
        if (string.IsNullOrEmpty(code))
        {
            if (statusText != null) statusText.text = "Enter a join code first.";
            Debug.LogError("Join code empty.");
            return;
        }

        StartClient(code);
    }

    public async void StartClient(string joinCode)
    {
        const string connectionType = "dtls";

        if (statusText != null) statusText.text = $"Joining...\nCode: {joinCode}";

        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));

        bool ok = NetworkManager.Singleton.StartClient();
        Debug.Log("StartClient() => " + ok);

        if (!ok && statusText != null) statusText.text = "Client failed to start (see Console).";
    }
}
