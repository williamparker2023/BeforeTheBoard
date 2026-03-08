using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

[RequireComponent(typeof(NetworkTransform))]
public class PlayerAiming : NetworkBehaviour
{

    private Camera mainCam;
    private Vector3 mousePos;

    // Firing Variables
    public GameObject bullet;
    public Transform bulletTransform;
    public bool canFire;
    [SerializeField] private float timer;
    [SerializeField] float TIME_BETWEEN_SHOTS = 3.0f;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(!IsOwner) return;

        mainCam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        if(!IsOwner) return;
        
        mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Vector3 aimDirection = mousePos - transform.position;
        float rotZ = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, rotZ);

        if (!canFire)
        {
            timer += Time.deltaTime;
            if (timer >= TIME_BETWEEN_SHOTS)
            {
                canFire = true;
                timer = 0.0f;
            }
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
            if (IsClient)
            {
                RequestSpawnServerRpc();
            }
            else
            {
                // Instantiate(bullet, bulletTransform.position, Quaternion.identity);
                var instance = Instantiate(bullet, bulletTransform.position, Quaternion.identity);
                var instanceNetworkObject = instance.GetComponent<NetworkObject>();
                instanceNetworkObject.Spawn();
            }
        }
    }

    [ServerRpc]
    private void RequestSpawnServerRpc(ServerRpcParams rpcParams = default)
    {
        // Only server executes this code
        GameObject spawnedObject = Instantiate(bullet, bulletTransform.position, Quaternion.identity);
        spawnedObject.GetComponent<NetworkObject>().Spawn();
    }
}
