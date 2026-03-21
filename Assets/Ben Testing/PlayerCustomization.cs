using UnityEngine;
using Unity.Netcode;

public class PlayerCustomization : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject classSelectionUI;
    [SerializeField] private GameObject meleeUpgradesUI;
    [SerializeField] private GameObject rangeUpgradesUI;


    
    public override void OnNetworkSpawn()
    {
        // Every player should see their own class selection UI when they spawn
        classSelectionUI.SetActive(true);
        meleeUpgradesUI.SetActive(false);
        rangeUpgradesUI.SetActive(false);
    }

    public void SetPlayerClassRange()
    {
        // Only the owner can request their own class change
        if (IsOwner)
        {
            RequestSetPlayerClassServerRpc(0); // 0 = Ranged
        }
    }

    public void SetPlayerClassMelee()
    {
        // Only the owner can request their own class change
        if (IsOwner)
        {
            RequestSetPlayerClassServerRpc(1); // 1 = Melee
        }
    }

    void UpdateUIForClassSelection(int classID)
    {
        if (!IsOwner) return;

        classSelectionUI.SetActive(false);
        
        if (classID == 0) // Ranged
        {
            rangeUpgradesUI.SetActive(true);
            meleeUpgradesUI.SetActive(false);
        }
        else if (classID == 1) // Melee
        {
            meleeUpgradesUI.SetActive(true);
            rangeUpgradesUI.SetActive(false);
        }
    }

    NetworkObject GetCurrentPlayer()
    {
        return NetworkManager.Singleton.LocalClient.PlayerObject;
    }

    [ServerRpc]
    private void RequestSetPlayerClassServerRpc(int classID, ServerRpcParams rpcParams = default)
    {
        // Server receives the request and updates the NetworkVariable
        // This syncs the change to all clients automatically
        var player = GetCurrentPlayer();
        var playerScript = player.GetComponent<BenPlayerTest>();
        if (playerScript != null)
        {
            playerScript.playerClassID.Value = classID;
            // Notify all clients via ClientRpc so UI updates immediately on owner's client
            OnPlayerClassChangedClientRpc(classID);
        }
    }

    [ClientRpc]
    private void OnPlayerClassChangedClientRpc(int classID)
    {
        // All clients update UI based on the new class selection
        UpdateUIForClassSelection(classID);
    }
}
