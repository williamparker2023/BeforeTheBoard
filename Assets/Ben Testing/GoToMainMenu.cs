using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Collections;

public class GoToMainMenu : NetworkBehaviour
{
    public void GoToMainButton()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        StartCoroutine(ShutdownAfterSceneLoad());
    }

    private IEnumerator ShutdownAfterSceneLoad()
    {
        // Wait one frame (or more if needed)
        yield return null;
        NetworkManager.Singleton.Shutdown();
    }
}
