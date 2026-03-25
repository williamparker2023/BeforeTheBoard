using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAppearance : NetworkBehaviour
{
    private SpriteRenderer sr;
    private BenPlayerTest playerTest;

    [SerializeField] private Sprite bishopSprite; // for ranged class (0)
    [SerializeField] private Sprite rookSprite; // for melee class (1)

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        playerTest = GetComponent<BenPlayerTest>();
    }

    public override void OnNetworkSpawn()
    {
        playerTest.playerClassID.OnValueChanged += OnClassChanged;
        UpdateSprite(playerTest.playerClassID.Value);
    }

    public override void OnNetworkDespawn()
    {
        if (playerTest != null)
        {
            playerTest.playerClassID.OnValueChanged -= OnClassChanged;
        }
    }

    private void OnClassChanged(int oldClass, int newClass)
    {
        UpdateSprite(newClass);
    }

    private void UpdateSprite(int classID)
    {
        if (sr != null)
        {
            sr.sprite = (classID == 0) ? bishopSprite : rookSprite;
        }
    }
}
