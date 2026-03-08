using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

[RequireComponent(typeof(NetworkTransform))]
public class BenPlayerTest : NetworkBehaviour
{

    // ============== Physics ==============
    Rigidbody2D rb = null;
    [SerializeField] float SPEED = 0.0f;
    // [SerializeField] Transform bulletTransform;

    // ============== Aiming ==============
    private Camera mainCam;
    private Vector3 mousePos;
    public GameObject bullet;
    public bool canFire;
    [SerializeField] private float timer;
    [SerializeField] float TIME_BETWEEN_SHOTS = 3.0f;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {        
        if (!IsOwner) return;

        rb = GetComponent<Rigidbody2D>();
        GetComponent<SpriteRenderer>().color = Color.green;
        mainCam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;

        if (!IsLocalPlayer)
        {
            return;
        }
        
        PlayerMovement();
        ShootProjectile();
    }

    void PlayerMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        
        Vector2 movement = new Vector2(horizontalInput * SPEED, verticalInput * SPEED);
        rb.linearVelocity = movement;

    }

    void ShootProjectile()
    {
        
        if (mainCam == null) mainCam = Camera.main;

        // Convert this local client's mouse screen position to world position
        Vector3 screenPos = Input.mousePosition;
        screenPos.z = Mathf.Abs(mainCam.transform.position.z - transform.position.z);
        mousePos = mainCam.ScreenToWorldPoint(screenPos);

        Vector3 aimDirection = mousePos - transform.position;
        float rotZ = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        // transform.rotation = Quaternion.Euler(0f, 0f, rotZ);

        if (!canFire)
        {
            timer += Time.deltaTime;
            if (timer >= TIME_BETWEEN_SHOTS)
            {
                canFire = true;
                timer = 0.0f;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) && canFire)
        {
            Quaternion spawnRot = transform.rotation;
            Vector3 spawnPos = transform.position;

            if (IsServer)
            {
                var instance = Instantiate(bullet, spawnPos, spawnRot);
                var instanceNetworkObject = instance.GetComponent<NetworkObject>();
                instanceNetworkObject.SpawnWithOwnership(OwnerClientId);
            }
            else if (IsClient)
            {
                RequestSpawnServerRpc(spawnPos, spawnRot);
            }
            canFire = false;
        }
    }

    [ServerRpc]
    private void RequestSpawnServerRpc(Vector3 spawnPos, Quaternion spawnRot, ServerRpcParams rpcParams = default)
    {
        GameObject spawnedObject = Instantiate(bullet, spawnPos, spawnRot);
        var netObj = spawnedObject.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(rpcParams.Receive.SenderClientId);
    }
}
