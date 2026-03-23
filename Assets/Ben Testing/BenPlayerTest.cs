using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.UI;

[RequireComponent(typeof(NetworkTransform))]
public class BenPlayerTest : NetworkBehaviour
{

    // [SerializeField] public bool isDead = false;
    [SerializeField] public NetworkVariable<bool> isDead = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] public NetworkVariable<int> playerClassID = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); //0 = ranged. 1 = melee


    [SerializeField] public NetworkVariable<FixedString64Bytes> playerUsername = new NetworkVariable<FixedString64Bytes>("User", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] public NetworkVariable<float> playerHealth = new NetworkVariable<float>(10.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] public Slider healthSlider; // Reference the Slider component

    [SerializeField] public NetworkVariable<float> playerMaxHealth = new NetworkVariable<float>(10.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ============== Physics ==============
    Rigidbody2D rb = null;
    [SerializeField] float SPEED = 0.0f;
    // [SerializeField] Transform bulletTransform;

    // ============== Aiming ==============
    private Camera mainCam;
    private Vector3 mousePos;

    // ============== BISHOP Shooting ==============
    // [SerializeField] public float rangeDamage = 0.5f;
    [SerializeField] public NetworkVariable<float> rangeDamage = new NetworkVariable<float>(0.5f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public GameObject bullet;
    public bool canFire;
    [SerializeField] private float timer;
    [SerializeField] float TIME_BETWEEN_SHOTS = 3.0f;

    // ============== KNIGHT Melee ==============
    [SerializeField] public NetworkVariable<float> meleeDamage = new NetworkVariable<float>(1.5f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    public GameObject meleeHitbox;
    public bool canMelee;
    [SerializeField] private float meleeTimer;
    [SerializeField] float TIME_BETWEEN_MELEE = 1.0f;

    // ============== Death Effect ============== 
    [SerializeField] GameObject deathParticle;

    // ============== LEVEL UP INFORMATION ==============
    [Header("Level Up Information")]
    [SerializeField] float hpIncrease = 10.0f; //Hp that increases with every level up
    [SerializeField] float rangeDmgIncrease = 1.5f;
    [SerializeField] float meleeDmgIncrease = 1.5f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(IsOwner)
        {
            GetComponent<SpriteRenderer>().color = Color.green;
        }

        rb = GetComponent<Rigidbody2D>();
        mainCam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

        playerUsername.OnValueChanged += OnPlayerUsernameChanged;
        UpdateUsernameText(playerUsername.Value.ToString());

        // Ensure all clients update the health bar when the networked health value changes
        playerHealth.OnValueChanged += OnPlayerHealthChanged;
        UpdateHealthBar();

        if (!IsOwner) return;

        string chosenName = "Player";
        if (ConnectionManager.Instance != null)
        {
            chosenName = ConnectionManager.Instance.PlayerName;
        }

        if (IsServer)
        {
            SetPlayerName(chosenName);
        }
        else
        {
            RequestSetPlayerNameServerRpc(chosenName);
        }
    }

    public override void OnDestroy()
    {
        playerUsername.OnValueChanged -= OnPlayerUsernameChanged;
        playerHealth.OnValueChanged -= OnPlayerHealthChanged;
        base.OnDestroy();
    }

    private void OnPlayerUsernameChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        UpdateUsernameText(newValue.ToString());
    }

    private void UpdateUsernameText(string name)
    {
        if (usernameText != null)
        {
            usernameText.text = name;
        }
    }

    void SetPlayerName(string name)
    {
        if (IsServer)
        {
            playerUsername.Value = name;
            UpdateUsernameText(name);
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

        if (!isDead.Value)
        {
            PlayerMovement();

            if(playerClassID.Value == 0) //If ranged
            {
                ShootProjectile();
            }
            else if(playerClassID.Value == 1) //If melee
            {
                MeleeAttack();
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(0, 0); //stop movement when dead
        }

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

        if (Input.GetMouseButton(0) && canMelee)
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

                MeleeAttack meleeScript = instance.GetComponent<MeleeAttack>(); //Set damage on the melee hitbox
                meleeScript.Initialize(meleeDamage.Value);
                
                instanceNetworkObject.SpawnWithOwnership(OwnerClientId);
            }
            else if (IsClient)
            {
                RequestMeleeSpawnServerRpc(spawnPos, spawnRot, meleeDamage.Value);
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

                ProjectileTest projScript = instance.GetComponent<ProjectileTest>(); //Set damage on the projectile
                projScript.Initialize(rangeDamage.Value);

                instanceNetworkObject.SpawnWithOwnership(OwnerClientId);
            }
            else if (IsClient)
            {
                RequestSpawnServerRpc(spawnPos, spawnRot);
            }
            canFire = false;
        }
    }

    public void KillPlayer()
    {
        if (!IsServer) return;

        isDead.Value = true;
        gameObject.tag = "DeadPlayer";
        playerHealth.Value = 0;
        Debug.Log("Player " + OwnerClientId + " has died.");

        GetComponent<SpriteRenderer>().color = Color.black; //Death visual indicator

        // Spawn death effect on all clients (including host)
        SpawnDeathEffectClientRpc(gameObject.transform.position, Quaternion.identity);

        // gameObject.SetActive(false);
        // NetworkObject.Despawn(true);
    }

    public void RevivePlayer()
    {
        if (!IsServer) return;
        gameObject.tag = "Player";
        playerHealth.Value = playerMaxHealth.Value;
        Debug.Log("Player " + OwnerClientId + " has been revived.");
        if(IsOwner)
        {
            GetComponent<SpriteRenderer>().color = Color.green; //Revive visual indicator

        }
        else
        {
            GetComponent<SpriteRenderer>().color = Color.white; //Revive visual indicator
        }
        isDead.Value = false;
    }

    public void TakeDamage(float damage)
    {
        if (!IsServer || isDead.Value) return;



        playerHealth.Value -= damage;
        UpdateHealthBar();
        if (playerHealth.Value <= 0)
        {
            this.KillPlayer();
            // Debug.Log("Player " + OwnerClientId + " has died.");
            // gameObject.SetActive(false);
            // NetworkObject.Despawn(true);
        }
    }

    public void LevelUp()
    {
        if(playerClassID.Value == 0) // Ranged
        {
            playerMaxHealth.Value = playerMaxHealth.Value + hpIncrease;
        }
        else // Melee
        {
            playerMaxHealth.Value = playerMaxHealth.Value + hpIncrease + 0.5f;
        }
        
        meleeDamage.Value = meleeDamage.Value + meleeDmgIncrease;
        rangeDamage.Value = rangeDamage.Value + rangeDmgIncrease;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        if (collision.gameObject.CompareTag("EnemyRange"))
        {
            TakeDamage(collision.gameObject.GetComponent<EnemyProjectileCode>().damage);
            collision.gameObject.GetComponent<NetworkObject>().Despawn(true);
            // Debug.Log("Player hit! Current health: " + playerHealth.Value);
            Destroy(collision.gameObject);
        }
        else if (collision.gameObject.CompareTag("EnemyMelee"))
        {
            TakeDamage(collision.gameObject.GetComponent<EnemyMeleeCode>().damage);
            collision.gameObject.GetComponent<NetworkObject>().Despawn(true);
            // Debug.Log("Player hit! Current health: " + playerHealth.Value);
            Destroy(collision.gameObject);
        }
    }

    public override void OnNetworkDespawn()
    {
        gameObject.SetActive(false);
        base.OnNetworkDespawn();
    }

    private void OnPlayerHealthChanged(float previousValue, float newValue)
    {
        UpdateHealthBar();
    }

    public void UpdateHealthBar()
    {
        if (healthSlider != null)
        {
            healthSlider.value = playerHealth.Value / playerMaxHealth.Value;
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
    private void RequestMeleeSpawnServerRpc(Vector3 spawnPos, Quaternion spawnRot, float damage, ServerRpcParams rpcParams = default)
    {
        GameObject spawnedObject = Instantiate(meleeHitbox, spawnPos, spawnRot);

        MeleeAttack meleeScript = spawnedObject.GetComponent<MeleeAttack>();
        if (meleeScript != null)
        {
            meleeScript.Initialize(damage);
        }

        var netObj = spawnedObject.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(rpcParams.Receive.SenderClientId);
    }

    [ServerRpc]
    private void RequestSetPlayerNameServerRpc(string name, ServerRpcParams rpcParams = default)
    {
        playerUsername.Value = name;
        usernameText.text = playerUsername.Value.ToString();
    }

    [ClientRpc]
    private void SpawnDeathEffectClientRpc(Vector3 spawnPos, Quaternion spawnRot)
    {
        if (deathParticle == null) return;
        Instantiate(deathParticle, spawnPos, spawnRot);
    }
}
