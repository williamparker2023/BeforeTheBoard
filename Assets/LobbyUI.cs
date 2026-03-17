using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;

public class LobbyUI : MonoBehaviour
{
    public TMP_Text codeText;
    public TMP_Text playerListText;
    public Button startButton;

    private LobbyPlayerManager subscribedManager;

    private void Start()
    {
        RefreshCodeText();
        RefreshStartButton();
        RefreshPlayerList();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientChanged;
        }

        StartCoroutine(WaitForLobbyManager());
    }

    private IEnumerator WaitForLobbyManager()
    {
        float timer = 0f;
        while (LobbyPlayerManager.Instance == null && timer < 5f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (LobbyPlayerManager.Instance != null)
        {
            subscribedManager = LobbyPlayerManager.Instance;
            subscribedManager.players.OnListChanged += OnPlayerListChanged;
            RefreshPlayerList();
        }
        else
        {
            Debug.LogError("LobbyPlayerManager never appeared in Lobby scene.");
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientChanged;
        }

        if (subscribedManager != null)
        {
            subscribedManager.players.OnListChanged -= OnPlayerListChanged;
        }
    }

    private void OnClientChanged(ulong clientId)
    {
        RefreshAll();
    }

    private void OnPlayerListChanged(NetworkListEvent<PlayerData> changeEvent)
    {
        RefreshPlayerList();
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
        if (playerListText == null) return;

        playerListText.text = "Players:\n";

        if (LobbyPlayerManager.Instance == null)
        {
            playerListText.text += "Loading...";
            return;
        }

        foreach (var player in LobbyPlayerManager.Instance.players)
        {
            playerListText.text += $"{player.playerName}\n";
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
}