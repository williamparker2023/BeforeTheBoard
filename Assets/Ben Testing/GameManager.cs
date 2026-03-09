using Unity.Netcode;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class GameManager : NetworkBehaviour
{
    [SerializeField] public NetworkVariable<int> netScore = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
}
