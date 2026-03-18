using UnityEngine;
using Unity.Netcode;

public class ReviveAllPlayersTest : NetworkBehaviour
{
    public void ReviveAllPlayers()
    {
        var players = NetworkManager.Singleton.ConnectedClients.Values;

        foreach (var playerClient in players)
        {
            if (playerClient.PlayerObject != null && playerClient.PlayerObject.CompareTag("DeadPlayer")) // Only consider dead players
            {
                playerClient.PlayerObject.GetComponent<BenPlayerTest>().RevivePlayer();
            }
        }
    }
}
