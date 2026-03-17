using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class LobbyUI : MonoBehaviour
{
    public TMP_Text codeText;
    public TMP_Text playerListText;
    public Button startButton;

    void Start()
    {
        if (ConnectionManager.Instance != null)
        {
            codeText.text = "Code: " + ConnectionManager.Instance.JoinCode;
        }

        if (NetworkManager.Singleton != null)
        {
            startButton.gameObject.SetActive(NetworkManager.Singleton.IsHost);
        }

        RefreshPlayerList();
    }

    void RefreshPlayerList()
    {
        if (playerListText == null || NetworkManager.Singleton == null) return;

        playerListText.text = "Players:\n";

        foreach (ulong id in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (id == NetworkManager.Singleton.LocalClientId && ConnectionManager.Instance != null)
            {
                playerListText.text += ConnectionManager.Instance.PlayerName + "\n";
            }
            else
            {
                playerListText.text += $"Player {id}\n";
            }
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