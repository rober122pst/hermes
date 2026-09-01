using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }
    public List<Enemy.EnemySystem> enemiesInWave = new List<Enemy.EnemySystem>();
    public int currWave;
    private int waveValue;
    public int multWaveValue = 10;
    
    public List<GameObject> enemiesToSpawn = new List<GameObject>();
    public ObjectPool enemyPool;

    public Transform spawnLocation;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        GenerateWave();
    }

    void GenerateWave()
    {
        waveValue = currWave * multWaveValue;
    }
}