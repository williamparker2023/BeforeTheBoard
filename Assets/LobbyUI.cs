using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class LobbyUI : MonoBehaviour
{
    public TMP_Text codeText;
    public TMP_Text playerListText;
    public Button startButton;

    private void Start()
    {
        RefreshAll();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientChanged;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientChanged;
        }
    }

    private void OnClientChanged(ulong clientId)
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        RefreshCodeText();
        RefreshStartButton();
        RefreshPlayerList();
    }

    private void RefreshCodeText()
    {
        if (codeText == null) return;

        if (ConnectionManager.Instance != null)
            codeText.text = "Code: " + ConnectionManager.Instance.JoinCode;
        else
            codeText.text = "Code: ?";
    }

    private void RefreshStartButton()
    {
        if (startButton == null || NetworkManager.Singleton == null) return;

        startButton.gameObject.SetActive(NetworkManager.Singleton.IsHost);
    }

    private void RefreshPlayerList()
    {
        if (playerListText == null || NetworkManager.Singleton == null) return;

        playerListText.text = "Players:\n";

        foreach (ulong id in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (id == NetworkManager.Singleton.LocalClientId && ConnectionManager.Instance != null)
                playerListText.text += ConnectionManager.Instance.PlayerName + "\n";
            else
                playerListText.text += $"Player {id}\n";
        }
    }

    public void OnStartGameClicked()
    {
        ConnectionManager.Instance.StartGame();
    }

    public void OnLeaveClicked()
    {
        ConnectionManager.Instance.LeaveLobby();
    }

    private void OnEnable()
    {
        Invoke(nameof(RefreshAll), 0.1f);
    }
}