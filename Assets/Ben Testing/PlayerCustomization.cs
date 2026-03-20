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
        if (IsOwner)
        {
            classSelectionUI.SetActive(true);
        }
    }

    public void SetPlayerClassRange()
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer != null)
        {
            currentPlayer.GetComponent<BenPlayerTest>().playerClassID.Value = 0; // Set to ranged
            HideClassSelectionUI();
            ShowUpgradeUI();
        }
    }

    public void SetPlayerClassMelee()
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer != null)
        {
            currentPlayer.GetComponent<BenPlayerTest>().playerClassID.Value = 1; // Set to melee
            HideClassSelectionUI();
            ShowUpgradeUI();
        }
    }

    void HideClassSelectionUI()
    {
        if (IsOwner)
        {
            classSelectionUI.SetActive(false);
        }
    }

    void ShowUpgradeUI()
    {
        var currentPlayer = GetCurrentPlayer();
        if (IsOwner && currentPlayer != null)
        {
            if(currentPlayer.GetComponent<BenPlayerTest>().playerClassID.Value == 0) //Ranged
            {
                rangeUpgradesUI.SetActive(true);
                meleeUpgradesUI.SetActive(false);
            }
            else if(currentPlayer.GetComponent<BenPlayerTest>().playerClassID.Value == 1) //Melee
            {
                meleeUpgradesUI.SetActive(true);
                rangeUpgradesUI.SetActive(false);
            }
        }
    }

    NetworkObject GetCurrentPlayer()
    {
        return NetworkManager.Singleton.LocalClient.PlayerObject;
    }
}
