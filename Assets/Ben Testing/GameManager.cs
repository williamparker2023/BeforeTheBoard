using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Components;
using TMPro;
using UnityEngine.UI;
using System.Xml.Serialization;

public class GameManager : NetworkBehaviour
{
    [Header("Backgrounds")]
    [SerializeField] public Sprite[] backgrounds;

    [Header("SpawnLocations")]
    [SerializeField] public BoxCollider2D rightSideSpawn;
    [SerializeField] public BoxCollider2D healthPackSpawn;
    [SerializeField] public GameObject enemyPrefab;
    [SerializeField] public GameObject healthPackPrefab;

    [Header("Player Info")]
    //Player experience, for leveling up
    [SerializeField] public NetworkVariable<int> playerXP = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] public NetworkVariable<int> XPNeeded = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] public NetworkVariable<int> playerLevel = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private int XP_NEEDED_PER_LEVEL = 100;

    [Header("Waves/Game Progression")]
    //If this is true, the game will continue through the waves. if its false, its assuming the players are leveling up, or otherwise not in a round yet
    [SerializeField] public NetworkVariable<bool> gameRunning = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    //Every 4th wave is a boss wave, so we'll use this to determine when to spawn bosses
    [SerializeField] public NetworkVariable<int> currentWave = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] public TMP_Text waveText;
    [SerializeField] public Slider xpSlider;

    public override void OnNetworkSpawn()
    {
        playerXP.OnValueChanged += OnXPChanged;
        XPNeeded.OnValueChanged += OnXPChanged;
        UpdateXPSlider();
    }

    public override void OnNetworkDespawn()
    {
        playerXP.OnValueChanged -= OnXPChanged;
        XPNeeded.OnValueChanged -= OnXPChanged;
    }

    private void OnXPChanged<T>(T oldValue, T newValue)
    {
        UpdateXPSlider();
    }

    void Update()
    {
        UpdateWaveCount();
        if (!IsServer) return;

        if (!gameRunning.Value) return;
        //============IF THE GAME IS RUNNING============
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("WaveEnemy");
        int enemyNum = 0;
        if (enemies != null)
        {
            enemyNum = enemies.Length;
        }

        GameObject[] bosses = GameObject.FindGameObjectsWithTag("BossEnemy");
        int bossNum = 0;
        if (bosses != null)
        {
            bossNum = bosses.Length;
        }

        if (enemyNum == 0 && bossNum == 0) //If there are no more enemies alive... (WE CAN CHANGE THIS TO WORK ON A TIMER INSTEAD. IT DONT MATTER ATM)
        {
            if (currentWave.Value % 4 == 0) //if it was a boss wave
            {
                ReviveAllPlayers();
            }

            //First, check if the players need to level up
            if (playerXP.Value >= XPNeeded.Value)
            {
                playerLevel.Value++;
                gameRunning.Value = false; //Pause game cycle
                playerXP.Value = 0;
                XPNeeded.Value += XP_NEEDED_PER_LEVEL; //Increase XP needed for next level, this is just an example
                
                LevelUp();
                UpdateXPSlider();
            }
            //Once finished leveling up continue to the next wave

            currentWave.Value++;
            UpdateWaveCount();
            RunWave(currentWave.Value);
        }
    }

    void UpdateWaveCount()
    {
        waveText.text = "Wave: " + currentWave.Value.ToString();
    }

    public void AddXP(int xp)
    {
        playerXP.Value += xp;
        UpdateXPSlider();
    }

    void UpdateXPSlider()
    {
        if (xpSlider != null) xpSlider.value = (float)playerXP.Value / XPNeeded.Value;
    }

    void RunWave(int waveNum)
    {
        if (waveNum % 4 == 0) //if BOSS WAVE
        {
            //Spawn a boss
            Debug.Log("Boss Wave! Spawning Boss...");
            
        }
        else //if a normal wave enemies wave
        {
            //============SPAWN WAVE ENEMIES============
            //Randomize number of enemies based on wave number & player count
            int numOfEnemies = waveNum + (waveNum / 2);

            //randomize spawn direction of enemies
            Vector2 colliderWorldCenter = (Vector2)rightSideSpawn.transform.position + rightSideSpawn.offset;

            float width, height, randomPosX, randomPosY;

            //Spawn Enemies
            for (int i = 0; i < numOfEnemies; i++)
            {
                // Calculate bounds taking into account the scale of the object
                width = rightSideSpawn.size.x * rightSideSpawn.transform.lossyScale.x;
                height = rightSideSpawn.size.y * rightSideSpawn.transform.lossyScale.y;

                randomPosX = Random.Range(colliderWorldCenter.x - width / 2f, colliderWorldCenter.x + width / 2f);
                randomPosY = Random.Range(colliderWorldCenter.y - height / 2f, colliderWorldCenter.y + height / 2f);
                Vector2 randomPos = new Vector2(randomPosX, randomPosY);

                var instance = Instantiate(enemyPrefab, randomPos, Quaternion.identity);

                if (IsServer)
                {
                    var instanceNetworkObject = instance.GetComponent<NetworkObject>();
                    instanceNetworkObject.SpawnWithOwnership(OwnerClientId);

                }
                else if (IsClient)
                {
                    RequestSpawnServerRpc(randomPos, Quaternion.identity);
                }
            }

            //Spawn HealthPacks
            for (int i = 0; i < waveNum/2; i++)
            {
                // Calculate bounds taking into account the scale of the object
                width = healthPackSpawn.size.x * healthPackSpawn.transform.lossyScale.x;
                height = healthPackSpawn.size.y * healthPackSpawn.transform.lossyScale.y;

                // Calculate center position for health pack spawn collider
                Vector2 healthPackColliderWorldCenter = (Vector2)healthPackSpawn.transform.position + healthPackSpawn.offset;

                randomPosX = Random.Range(healthPackColliderWorldCenter.x - width / 2f, healthPackColliderWorldCenter.x + width / 2f);
                randomPosY = Random.Range(healthPackColliderWorldCenter.y - height / 2f, healthPackColliderWorldCenter.y + height / 2f);
                Vector2 randomPos = new Vector2(randomPosX, randomPosY);

                var instance = Instantiate(healthPackPrefab, randomPos, Quaternion.identity);

                if (IsServer)
                {
                    var instanceNetworkObject = instance.GetComponent<NetworkObject>();
                    instanceNetworkObject.SpawnWithOwnership(OwnerClientId);

                }
                else if (IsClient)
                {
                    RequestSpawnHPServerRpc(randomPos, Quaternion.identity);
                }
            }
        }
    }

    void LevelUp()
    {
        Debug.Log("Players leveled up to level " + playerLevel.Value + "!");
        var players = NetworkManager.Singleton.ConnectedClients.Values;

        foreach (var playerClient in players)
        {
            if (playerClient.PlayerObject != null) // Only consider dead players
            {
                playerClient.PlayerObject.GetComponent<BenPlayerTest>().LevelUp();
            }
        }

        //Go through each player, level them up
        gameRunning.Value = true;
    }

    public void ReviveAllPlayers()
    {
        var players = NetworkManager.Singleton.ConnectedClients.Values;

        foreach (var playerClient in players)
        {
            if (playerClient.PlayerObject != null && playerClient.PlayerObject.CompareTag("DeadPlayer")) // Only consider dead players
            {
                playerClient.PlayerObject.GetComponent<BenPlayerTest>().RevivePlayer();
            }
        }
    }

    [ServerRpc]
    private void RequestSpawnServerRpc(Vector3 spawnPos, Quaternion spawnRot, ServerRpcParams rpcParams = default)
    {
        GameObject spawnedObject = Instantiate(enemyPrefab, spawnPos, spawnRot);
        var netObj = spawnedObject.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(rpcParams.Receive.SenderClientId);
    }

    [ServerRpc]
    private void RequestSpawnHPServerRpc(Vector3 spawnPos, Quaternion spawnRot, ServerRpcParams rpcParams = default)
    {
        GameObject spawnedObject = Instantiate(healthPackPrefab, spawnPos, spawnRot);
        var netObj = spawnedObject.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(rpcParams.Receive.SenderClientId);
    }
}
