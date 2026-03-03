using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAppearance : NetworkBehaviour
{
    private SpriteRenderer sr;

    private NetworkVariable<Color32> netColor = new NetworkVariable<Color32>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // default for everyone based on OwnerClientId
            byte r = (byte)(50 + (OwnerClientId * 70) % 200);
            byte g = (byte)(50 + (OwnerClientId * 120) % 200);
            byte b = (byte)(50 + (OwnerClientId * 170) % 200);
            netColor.Value = new Color32(r, g, b, 255);
        }

        if (IsOwner)
        {
            SetMyColorServerRpc(new Color32(255, 0, 0, 255)); // my cube red
        }
    }

    public override void OnNetworkDespawn()
    {
        netColor.OnValueChanged -= OnColorChanged;
    }

    private void OnColorChanged(Color32 oldColor, Color32 newColor)
    {
        ApplyColor(newColor);
    }

    private void ApplyColor(Color32 c)
    {
        if (sr != null)
            sr.color = c;
    }

    [ServerRpc]
    private void SetMyColorServerRpc(Color32 color)
    {
        netColor.Value = color;
    }
}