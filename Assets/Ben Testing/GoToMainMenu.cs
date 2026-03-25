using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class GoToMainMenu : NetworkBehaviour
{
    public void GoToMainButton()
    {
        // NetworkManager.Singleton.Shutdown();
        // SceneManager.LoadScene("MainMenu");
        NetworkManager.Singleton.SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        NetworkManager.Singleton.Shutdown();

        // Application.Quit();
        //  System.Diagnostics.Process.Start(Application.dataPath.Replace("_Data", ".exe")); //new program Application.Quit(); //kill current process
    }
}
