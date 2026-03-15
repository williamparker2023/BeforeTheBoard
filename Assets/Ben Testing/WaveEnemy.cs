using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Components;
using System.Collections;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
// using System.Numerics;

[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(NetworkObject))]
public class WaveEnemy : NetworkBehaviour
{
    //======= VARIABLES TO RANDOMIZE
    [SerializeField] bool isAggressive = true; //If true, then timid


    [SerializeField] bool meleeType = true; //false = projectile
    [SerializeField] int classType = 0; // 0 = dps (high damage/speed, low health), 1 = tank (low damage/speed, high health)

    [SerializeField] public NetworkVariable<float> enemyHealth = new NetworkVariable<float>(10.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    //======== SHOOTING
    [SerializeField] public float rangeDamage = 0.5f;
    [SerializeField] GameObject projectilePrefab;
    public bool canFire;
    [SerializeField] private float timer;
    [SerializeField] float TIME_BETWEEN_SHOTS = 3.0f;

    //======== MOVEMENT
    [SerializeField] float SPEED = 5.0f;
    [SerializeField] float WORLD_LIMIT = 4.0f; // The Y limit where the enemy will stop moving (prevents it from leaving the game space)
    [SerializeField] float WORLD_X_LIMIT = 8.0f; // The X limit where the enemy will stop moving (prevents it from leaving the game space)

    // Ranged Movement
    [SerializeField] float STOP_DISTANCE = 5.0f; // distance the enemy stops moving towards the player (aggressive)
    [SerializeField] float RETREAT_DISTANCE = 3.0f; // distance the enemy will start retreating if the player approaches
    [SerializeField] float TIMID_DISTANCE = 6.0f; // distance a timid enemy will try to maintain from the player

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
            RangeMovement();
            // if (!isMoving)
            // {
                // StartCoroutine(RangeMovement());
            // }
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
            // Debug.Log("Enemy hit! Current health: " + enemyHealth.Value);


            if (enemyHealth.Value <= 0)
            {
                KillEnemy();
            }
        }
    }

    private void KillEnemy()
    {
        if (!IsServer) return;
        NetworkObject.Despawn(true);
    }

    public override void OnNetworkDespawn()
    {
        gameObject.SetActive(false);
        base.OnNetworkDespawn();
    }

    void RangeMovement()
    {
        Transform nearestPlayer = GetClosestPlayer();
        if (nearestPlayer == null)
        {
            return; // No players left, so don't move
        }

        if(!IsServer) return;

        // Making sure the enemy isnt out of bounds
        if (transform.position.y < -WORLD_LIMIT)
        {
            transform.position = new Vector2(transform.position.x, -WORLD_LIMIT + 0.1f);
            // Debug.Log("Out of bounds!");
            return;
        }
        else if (transform.position.y > WORLD_LIMIT)
        {
            transform.position = new Vector2(transform.position.x, WORLD_LIMIT - 0.1f);
            // Debug.Log("Out of bounds!");
            return;
        }
        if(transform.position.x < -WORLD_X_LIMIT)
        {
            transform.position = new Vector2(-WORLD_X_LIMIT + 0.1f, transform.position.y);
            // Debug.Log("Out of bounds!");
            return;
        }
        else if (transform.position.x > WORLD_X_LIMIT)
        {
            transform.position = new Vector2(WORLD_X_LIMIT - 0.1f, transform.position.y);
            // Debug.Log("Out of bounds!");
            return;
        }

        // Making sure the enemy isnt in another enemy. If it is, move a little bit away from the closest enemy
        if (GetClosestEnemy() != null && Vector2.Distance(transform.position, GetClosestEnemy().transform.position) < 0.1f)
        {
            transform.position = Vector2.MoveTowards(transform.position, GetClosestEnemy().transform.position, -SPEED * Time.deltaTime);
            // Debug.Log("lwk stunlocked probably");
            return;
        }

        if (isAggressive) //Aggressive: Keep a relative distance from the nearest player (STOP_DISTANCE / RETREAT DISTANCE)
        {
            if(Vector2.Distance(transform.position, nearestPlayer.position) > STOP_DISTANCE)
            {
                transform.position = Vector2.MoveTowards(transform.position, nearestPlayer.position, SPEED * Time.deltaTime);
            }
            else if(Vector2.Distance(transform.position, nearestPlayer.position) < STOP_DISTANCE && Vector2.Distance(transform.position, nearestPlayer.position) > RETREAT_DISTANCE)
            {
                transform.position = this.transform.position;
            }
            else if(Vector2.Distance(transform.position, nearestPlayer.position) < RETREAT_DISTANCE)
            {
                transform.position = Vector2.MoveTowards(transform.position, nearestPlayer.position, -SPEED * Time.deltaTime);
            }
        } else //Timid: Stay as far from players as possible (TIMID_DISTANCE)
        {
            if(Vector2.Distance(transform.position, nearestPlayer.position) < TIMID_DISTANCE)
            {
                transform.position = Vector2.MoveTowards(transform.position, nearestPlayer.position, -SPEED * Time.deltaTime);
            }
            else
            {
                transform.position = this.transform.position;
            }
        }
        // Debug.Log("Got to end of movement without moving.. huh");
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
            if (closestPlayer == null)
            {
                return; // No players left, so don't move
            }

            Vector2 direction = closestPlayer.position - transform.position;
        
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(new Vector3(0, 0, angle-90));

            if (IsServer)
            {
                var instance = Instantiate(projectilePrefab, transform.position, targetRotation);
                var instanceNetworkObject = instance.GetComponent<NetworkObject>();
                EnemyProjectileCode projScript = instance.GetComponent<EnemyProjectileCode>(); //Set damage on the projectile
                projScript.Initialize(rangeDamage);
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

        EnemyProjectileCode projScript = instance.GetComponent<EnemyProjectileCode>(); //Set damage on the projectile
        projScript.Initialize(rangeDamage);

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
        // Debug.Log("Closest player: " + closestPlayer.name);
        return closestPlayer;
    }

    public GameObject GetClosestEnemy()
    {
        GameObject[] gameObjectsWithTag = GameObject.FindGameObjectsWithTag("WaveEnemy");
        GameObject closestObject = null;
        float minDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        foreach (GameObject obj in gameObjectsWithTag)
        {
            Vector3 directionToTarget = obj.transform.position - currentPosition;
            float distanceSqr = directionToTarget.sqrMagnitude;

            if (distanceSqr < minDistanceSqr && obj != gameObject) //making sure that the closest enemy isnt itself, lol
            {
                minDistanceSqr = distanceSqr;
                closestObject = obj;
            }
        }

        return closestObject;
    }
}
