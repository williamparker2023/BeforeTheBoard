using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Components;

public class GameManager : NetworkBehaviour
{
    [Header("Network Variables")]
    //Player experience, for leveling up
    [SerializeField] public NetworkVariable<int> playerXP = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] public NetworkVariable<int> XPNeeded = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] public NetworkVariable<int> playerLevel = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private int XP_NEEDED_PER_LEVEL = 100;

    [Header("Waves/Game Progression")]
    //If this is true, the game will continue through the waves. if its false, its assuming the players are leveling up, or otherwise not in a round yet
    [SerializeField] public NetworkVariable<bool> gameRunning = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    //Every 4th wave is a boss wave, so we'll use this to determine when to spawn bosses
    [SerializeField] public NetworkVariable<int> currentWave = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Update()
    {
        if (!IsServer) return;

        if (!gameRunning.Value) return;
        //============IF THE GAME IS RUNNING============

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("WaveEnemy");
        int enemyNum = 0;
        if(enemies != null)
        {
            enemyNum = enemies.Length;
        }

        GameObject[] bosses = GameObject.FindGameObjectsWithTag("BossEnemy");
        int bossNum = 0;
        if(bosses != null)
        {
            bossNum = bosses.Length;
        }

        if (enemyNum == 0 && bossNum == 0) //If there are no more enemies alive... (WE CAN CHANGE THIS TO WORK ON A TIMER INSTEAD. IT DONT MATTER ATM)
        {
            //First, check if the players need to level up
            if(playerXP.Value >= XPNeeded.Value)
            {
                playerLevel.Value++;
                gameRunning.Value = false; //Pause game cycle
                playerXP.Value = 0;
                XPNeeded.Value += XP_NEEDED_PER_LEVEL; //Increase XP needed for next level, this is just an example
                if (currentWave.Value % 4 == 0) //if a boss wave
                {
                    RevivePlayers();
                }
                LevelUp();
            }
            //Once finished leveling up continue to the next wave

            currentWave.Value++;
            RunWave(currentWave.Value);
        }
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
            //Randomize enemy type
            //Randomize number of enemies based on wave number & player count
            //randomize spawn direction of enemies
            //
        }
    }

    void LevelUp()
    {
        Debug.Log("Players leveled up to level " + playerLevel.Value + "!");
        //Spawn level up GUI
        while(true)
        {
                //Wait for player input to choose a level up option
                //Apply the chosen level up option to the player
                //Break out of the loop once the player has made their choice
                //Return false if issue
        }
        //gameRunning.Value = true;
    }

    void RevivePlayers()
    {
        Debug.Log("Reviving players for boss wave...");
        //Find all player objects and revive them to full health
    }
}
