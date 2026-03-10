using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Collections;
using TMPro;

[RequireComponent(typeof(NetworkTransform))]
public class BenPlayerTest : NetworkBehaviour
{

    [SerializeField] int playerClassID = 1; // 0 = bishop, 1 = knight, 2 = rook
    [SerializeField] public NetworkVariable<FixedString64Bytes> playerUsername = new NetworkVariable<FixedString64Bytes>("User", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] public NetworkVariable<float> playerHealth = new NetworkVariable<float>(10.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ============== Physics ==============
    Rigidbody2D rb = null;
    [SerializeField] float SPEED = 0.0f;
    // [SerializeField] Transform bulletTransform;

    // ============== Aiming ==============
    private Camera mainCam;
    private Vector3 mousePos;

    // ============== BISHOP Shooting ==============
    public GameObject bullet;
    public bool canFire;
    [SerializeField] private float timer;
    [SerializeField] float TIME_BETWEEN_SHOTS = 3.0f;

    // ============== KNIGHT Melee ==============
    public GameObject meleeHitbox;
    public bool canMelee;
    [SerializeField] private float meleeTimer;
    [SerializeField] float TIME_BETWEEN_MELEE = 1.0f;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {        
        if (!IsOwner) return;

        if(IsServer)
        {
            SetPlayerName("Player " + OwnerClientId);
        }
        else
        {
            RequestSetPlayerNameServerRpc("Player " + OwnerClientId);
        }

        rb = GetComponent<Rigidbody2D>();
        GetComponent<SpriteRenderer>().color = Color.green;
        mainCam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
    }

    void SetPlayerName(string name)
    {
        if (IsServer)
        {
            playerUsername.Value = name;
            usernameText.text = playerUsername.Value.ToString();
        }
        else
        {
            RequestSetPlayerNameServerRpc(name);
        }
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
        // if (playerClassID == 0) ShootProjectile();
        // if (playerClassID == 1) MeleeAttack();
        ShootProjectile();
        MeleeAttack();
    }

    void PlayerMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        
        Vector2 movement = new Vector2(horizontalInput * SPEED, verticalInput * SPEED);
        rb.linearVelocity = movement;

    }

    void MeleeAttack()
    {
        if (mainCam == null) mainCam = Camera.main;

        if (!canMelee)
        {
            meleeTimer += Time.deltaTime;
            if (meleeTimer >= TIME_BETWEEN_MELEE)
            {
                canMelee = true;
                meleeTimer = 0.0f;
            }
        }

        if (Input.GetMouseButton(1) && canMelee)
        {
            if (mainCam == null) mainCam = Camera.main;
            Vector3 screenPos = Input.mousePosition;
            screenPos.z = Mathf.Abs(mainCam.transform.position.z - transform.position.z);
            Vector3 mouseWorld = mainCam.ScreenToWorldPoint(screenPos);

            Vector3 aimDir = mouseWorld - transform.position;
            float rotZ = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
            Quaternion spawnRot = Quaternion.Euler(0f, 0f, rotZ + 90f);

            Vector3 spawnPos = transform.position - spawnRot * Vector3.up * 0.9f;

            if (IsServer)
            {
                var instance = Instantiate(meleeHitbox, spawnPos, spawnRot);
                var instanceNetworkObject = instance.GetComponent<NetworkObject>();
                instanceNetworkObject.SpawnWithOwnership(OwnerClientId);
            }
            else if (IsClient)
            {
                RequestMeleeSpawnServerRpc(spawnPos, spawnRot);
            }
            canMelee = false;
        }
    }

    void ShootProjectile()
    {
        
        if (mainCam == null) mainCam = Camera.main;

        if (!canFire)
        {
            timer += Time.deltaTime;
            if (timer >= TIME_BETWEEN_SHOTS)
            {
                canFire = true;
                timer = 0.0f;
            }
        }

        if (Input.GetMouseButton(0) && canFire)
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

    public void TakeDamage(float damage)
    {
        if (!IsServer) return;

        playerHealth.Value -= damage;

        if (playerHealth.Value <= 0)
        {
            playerHealth.Value = 0;
            Debug.Log("Player " + OwnerClientId + " has died.");
            gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        if (collision.gameObject.CompareTag("EnemyRange"))
        {
            TakeDamage(collision.gameObject.GetComponent<EnemyProjectileCode>().damage);
            collision.gameObject.GetComponent<NetworkObject>().Despawn(true);
            Debug.Log("Player hit! Current health: " + playerHealth.Value);
            Destroy(collision.gameObject);
        }
    }

  [ServerRpc]
    private void RequestSpawnServerRpc(Vector3 spawnPos, Quaternion spawnRot, ServerRpcParams rpcParams = default)
    {
        GameObject spawnedObject = Instantiate(bullet, spawnPos, spawnRot);
        var netObj = spawnedObject.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(rpcParams.Receive.SenderClientId);
    }

    [ServerRpc]
    private void RequestMeleeSpawnServerRpc(Vector3 spawnPos, Quaternion spawnRot, ServerRpcParams rpcParams = default)
    {
        GameObject spawnedObject = Instantiate(meleeHitbox, spawnPos, spawnRot);
        var netObj = spawnedObject.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(rpcParams.Receive.SenderClientId);
    }

    [ServerRpc]
    private void RequestSetPlayerNameServerRpc(string name, ServerRpcParams rpcParams = default)
    {
        playerUsername.Value = name;
        usernameText.text = playerUsername.Value.ToString();
    }
}
