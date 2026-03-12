using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Components;
using System.Collections;

[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(NetworkObject))]
public class WaveEnemy : NetworkBehaviour
{
    [SerializeField] bool meleeType = true; //false = projectile
    [SerializeField] int classType = 0; // 0 = dps (high damage/speed, low health), 1 = tank (low damage/speed, high health)

    [SerializeField] public NetworkVariable<float> enemyHealth = new NetworkVariable<float>(10.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    //======== SHOOTING
    [SerializeField] GameObject projectilePrefab;
    public bool canFire;
    [SerializeField] private float timer;
    [SerializeField] float TIME_BETWEEN_SHOTS = 3.0f;

    //======== MOVEMENT
    [SerializeField] float SPEED = 5.0f;
    [SerializeField] float WORLD_LIMIT = 4.5f;
    [SerializeField] float MOVE_NOISE = 0.5f;
    [SerializeField] float TIME_BETWEEN_MOVE_CHANGES = 3.0f;
    private bool isMoving = false;

    void Update()
    {
        if (!IsServer) return;

        if (meleeType)
        {
            // Handle melee attack logic here
        }
        else
        {
            Shoot();
            if (!isMoving)
            {
                StartCoroutine(RangeMovement());
            }
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        if (collision.gameObject.CompareTag("PlayerAttack"))
        {
            // Only server executes this code
            enemyHealth.Value -= collision.gameObject.GetComponent<ProjectileTest>().damage;
            collision.gameObject.GetComponent<NetworkObject>().Despawn(true);
            Debug.Log("Enemy hit! Current health: " + enemyHealth.Value);


            if (enemyHealth.Value <= 0)
            {
                KillEnemy();
            }
        }
    }

    private void KillEnemy()
    {
        if (!IsServer) return;
        NetworkObject.Despawn(false);
    }

    public override void OnNetworkDespawn()
    {
        gameObject.SetActive(false);
        base.OnNetworkDespawn();
    }

    IEnumerator RangeMovement()
    {
        isMoving = true;

        yield return new WaitForSeconds(TIME_BETWEEN_MOVE_CHANGES);

        Transform nearestPlayer = GetClosestPlayer();
        float randomNoise = Random.Range(-MOVE_NOISE, MOVE_NOISE);
        Vector2 targetPosition = new Vector2(transform.position.x, -nearestPlayer.position.y + randomNoise);
        
        float moveSpeed = SPEED * Time.deltaTime;

        if(transform.position.y > -WORLD_LIMIT && transform.position.y < WORLD_LIMIT)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed);
        }
        else if(transform.position.y < -WORLD_LIMIT)
        {
            targetPosition = new Vector2(transform.position.x, -WORLD_LIMIT + 0.1f);
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed);
        }
        else if (transform.position.y > WORLD_LIMIT)
        {
            targetPosition = new Vector2(transform.position.x, WORLD_LIMIT - 0.1f);
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed);
        }
        isMoving = false;
    }

    void Shoot()
    {
        if (!canFire)
        {
            timer += Time.deltaTime;
            if (timer >= TIME_BETWEEN_SHOTS)
            {
                canFire = true;
                timer = 0.0f;
            }
        }

        if(canFire)
        {
            Transform closestPlayer = GetClosestPlayer();
            Vector2 direction = closestPlayer.position - transform.position;
        
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(new Vector3(0, 0, angle-90));

            if (IsServer)
            {
                var instance = Instantiate(projectilePrefab, transform.position, targetRotation);
                var instanceNetworkObject = instance.GetComponent<NetworkObject>();
                instanceNetworkObject.SpawnWithOwnership(OwnerClientId);
            }
            else if (IsClient)
            {
                RequestShootServerRpc(transform.position, targetRotation);
            }
            canFire = false;
        }

    }

    [ServerRpc]
    void RequestShootServerRpc(Vector3 position, Quaternion rotation)
    {
        var instance = Instantiate(projectilePrefab, position, rotation);
        var instanceNetworkObject = instance.GetComponent<NetworkObject>();
        instanceNetworkObject.SpawnWithOwnership(OwnerClientId);
    }

    public Transform GetClosestPlayer()
    {
        var players = NetworkManager.Singleton.ConnectedClients.Values;
        Transform closestPlayer = null;
        float minDistance = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        foreach (var playerClient in players)
        {
            if (playerClient.PlayerObject != null)
            {
                // Calculate distance using sqrMagnitude for performance
                float distance = (playerClient.PlayerObject.transform.position - currentPosition).sqrMagnitude;
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPlayer = playerClient.PlayerObject.transform;
                }
            }
        }
        Debug.Log("Closest player: " + closestPlayer.name);
        return closestPlayer;
    }
}
