using UnityEngine;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    public TMP_InputField playerNameInput;
    public TMP_InputField joinCodeInput;
    public TMP_Text statusText;

    public void OnHostClicked()
    {
        ConnectionManager.Instance.SetPlayerName(playerNameInput.text);
        statusText.text = "Starting Host...";
        ConnectionManager.Instance.StartHost();
    }

    public void OnJoinClicked()
    {
        string code = joinCodeInput.text.Trim();

        if (string.IsNullOrEmpty(code))
        {
            statusText.text = "Enter a code.";
            return;
        }

        ConnectionManager.Instance.SetPlayerName(playerNameInput.text);
        statusText.text = $"Joining {code}...";
        ConnectionManager.Instance.StartClient(code);
    }
}