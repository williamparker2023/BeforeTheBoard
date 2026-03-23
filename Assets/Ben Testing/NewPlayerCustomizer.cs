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
        // Find the world game objects - try multiple possible paths
        classSelectionUI = GameObject.Find("ClassSelectionUI") ?? GameObject.Find("/Canvas/ClassSelectionUI") ?? GameObject.Find("Canvas/ClassSelectionUI");
        meleeUpgradesUI = GameObject.Find("MeleeUpgradesUI") ?? GameObject.Find("/Canvas/MeleeUpgradesUI") ?? GameObject.Find("Canvas/MeleeUpgradesUI");
        rangeUpgradesUI = GameObject.Find("RangeUpgradesUI") ?? GameObject.Find("/Canvas/RangeUpgradesUI") ?? GameObject.Find("Canvas/RangeUpgradesUI");
        loadingBar = GameObject.Find("LoadingBar") ?? GameObject.Find("/Canvas/LoadingBar") ?? GameObject.Find("Canvas/LoadingBar");

        // Debug logging
        Debug.Log($"Player {OwnerClientId} (IsOwner: {IsOwner}, IsServer: {IsServer}) - UI Found: ClassSelectionUI={classSelectionUI != null}, MeleeUpgradesUI={meleeUpgradesUI != null}, RangeUpgradesUI={rangeUpgradesUI != null}");

        if (classSelectionUI != null)
        {
            // Find buttons within the classSelectionUI - use relative paths
            Transform selectBishop = classSelectionUI.transform.Find("SelectBishop");
            Transform selectRook = classSelectionUI.transform.Find("SelectRook");

            if (selectBishop != null)
            {
                Button rangeButton = selectBishop.GetComponent<Button>();
                if (rangeButton != null)
                {
                    rangeButton.onClick.AddListener(SetPlayerClassRange);
                    Debug.Log("Range button listener added");
                }
            }

            if (selectRook != null)
            {
                Button meleeButton = selectRook.GetComponent<Button>();
                if (meleeButton != null)
                {
                    meleeButton.onClick.AddListener(SetPlayerClassMelee);
                    Debug.Log("Melee button listener added");
                }
            }
        }

        // Only set UI active if this is the local player (owner)
        if (IsOwner)
        {
            if (classSelectionUI != null) classSelectionUI.SetActive(true);
            if (meleeUpgradesUI != null) meleeUpgradesUI.SetActive(false);
            if (rangeUpgradesUI != null) rangeUpgradesUI.SetActive(false);
            if (loadingBar != null) loadingBar.SetActive(false);
        }
    }

    public void SetPlayerClassRange()
    {
        // Only the owner can request their own class change
        if (IsOwner)
        {
            RequestSetPlayerClassServerRpc(0); // 0 = Ranged
            UpdateUIForClassSelection(0);
        }
    }

    public void SetPlayerClassMelee()
    {
        // Only the owner can request their own class change
        if (IsOwner)
        {
            RequestSetPlayerClassServerRpc(1); // 1 = Melee
            UpdateUIForClassSelection(1);
        }
    }

    void UpdateUIForClassSelection(int classID)
    {
        if (!IsOwner) return;

        Debug.Log($"Updating UI for player {OwnerClientId} to class {classID}");

        if (classSelectionUI != null) classSelectionUI.SetActive(false);

        if (classID == 0) // Ranged
        {
            if (rangeUpgradesUI != null)
            {
                rangeUpgradesUI.SetActive(true);
                Debug.Log("Range upgrades UI activated for player " + OwnerClientId);
            }
            if (meleeUpgradesUI != null) meleeUpgradesUI.SetActive(false);
        }
        else if (classID == 1) // Melee
        {
            if (meleeUpgradesUI != null)
            {
                meleeUpgradesUI.SetActive(true);
                Debug.Log("Melee upgrades UI activated for player " + OwnerClientId);
            }
            if (rangeUpgradesUI != null) rangeUpgradesUI.SetActive(false);
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
