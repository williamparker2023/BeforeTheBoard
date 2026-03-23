using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class NewPlayerCustomizer : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject classSelectionUI;
    [SerializeField] private GameObject meleeUpgradesUI;
    [SerializeField] private GameObject rangeUpgradesUI;
    private GameObject loadingBar;

    public override void OnNetworkSpawn()
    {
        // Find the world game objects
        classSelectionUI = GameObject.Find("/Canvas/ClassSelectionUI");
        meleeUpgradesUI = GameObject.Find("/Canvas/MeleeUpgradesUI");
        rangeUpgradesUI = GameObject.Find("/Canvas/RangeUpgradesUI");

        // Find your buttons
        Button rangeButton = classSelectionUI.transform.Find("/Canvas/ClassSelectionUI/SelectBishop").GetComponent<Button>();
        Button meleeButton = classSelectionUI.transform.Find("/Canvas/ClassSelectionUI/SelectRook").GetComponent<Button>();

        // Add listeners
        rangeButton.onClick.AddListener(SetPlayerClassRange);
        meleeButton.onClick.AddListener(SetPlayerClassMelee);

        loadingBar = GameObject.Find("/Canvas/LoadingBar");

        classSelectionUI.SetActive(true);
        meleeUpgradesUI.SetActive(false);
        rangeUpgradesUI.SetActive(false);
        loadingBar.SetActive(false);
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

    // NetworkObject GetCurrentPlayer()
    // {
    //     return NetworkManager.Singleton.LocalClient.PlayerObject;
    // }

    [ServerRpc]
    private void RequestSetPlayerClassServerRpc(int classID, ServerRpcParams rpcParams = default)
    {
        // Server receives the request and updates the NetworkVariable for this player
        // Get BenPlayerTest from this same GameObject (PlayerCustomization is on the player)
        var playerScript = gameObject.GetComponent<BenPlayerTest>();
        if (playerScript != null)
        {
            playerScript.playerClassID.Value = classID;
            // Notify all clients via ClientRpc so UI updates immediately on owner's client
            OnPlayerClassChangedClientRpc(classID);
        }
        else
        {
            Debug.LogError("Player script not found on the Player GameObject!");
        }
    }

    [ClientRpc]
    private void OnPlayerClassChangedClientRpc(int classID)
    {
        // All clients update UI based on the new class selection
        UpdateUIForClassSelection(classID);
    }
}
