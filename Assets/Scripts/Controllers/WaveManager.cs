using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct EnemyPool
{
    public EnemyType type;
    public ObjectPool pool;
}

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    public List<EnemyPool> enemyPools = new List<EnemyPool>();
    private Dictionary<EnemyType, ObjectPool> enemyPoolDict;

    public List<EnemiesConfig> enemies = new List<EnemiesConfig>();
    public int currWave;
    private int waveValue;
    public List<List<EnemyType>> subWaves = new List<List<EnemyType>>();

    public int enemiesToSpawn;

    public Transform[] spawnLocation;
    public int spawnIndex;

   
    public int minTotalEnemies = 80;
    public int totalSubWaves;

    [SerializeField]
    private float spawnInterval = 2f;
    private float spawnTimer;

    public List<GameObject> spawnedEnemies = new List<GameObject>();

    private void Awake()
    {
        enemyPoolDict = new Dictionary<EnemyType, ObjectPool>();

        foreach (var enemyPool in enemyPools)
        {
            if (!enemyPoolDict.ContainsKey(enemyPool.type))
            {
                enemyPoolDict.Add(enemyPool.type, enemyPool.pool);
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
        GenerateWave();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (spawnTimer <= 0 && spawnedEnemies.Count < 50)
        {
            Debug.Log($"Spawning enemies. SpawnedEnemies: {spawnedEnemies.Count}, EnemiesToSpawn: {enemiesToSpawn}, SubWaves: {subWaves.Count}");
            if (subWaves.Count > 0)
            {
                foreach (var e in subWaves[0])
                {
                    if (enemyPoolDict.TryGetValue(e, out ObjectPool pool))
                    {
                        GameObject enemy = pool.GetInstance();
                        enemy.transform.position = spawnLocation[spawnIndex].position;
                        spawnIndex = (spawnIndex + 1) % spawnLocation.Length;
                        enemy.SetActive(true);
                        spawnedEnemies.Add(enemy);
                        ShipManager.Instance.AddActiveShip(enemy.transform);
                    }
                    else
                    {
                        Debug.LogWarning($"No ObjectPool found for EnemyType: {e}");
                    }
                    
                }
                subWaves.RemoveAt(0);
            }
            spawnTimer = spawnInterval;
        }
        else
        {
            spawnTimer -= Time.fixedDeltaTime;
        }
    }

    public void GenerateWave()
    {
        waveValue = CalcNewValue();
        GenerateEnemies();
    }

    public void GenerateEnemies()
    {
        List<EnemyType> generatedEnemies = new List<EnemyType>();
        while (waveValue > 0 || generatedEnemies.Count < minTotalEnemies + 50)
        {
            int randEnemyId = Random.Range(0, enemies.Count);
            int randEnemyCost = enemies[randEnemyId].cost;

            if (waveValue - randEnemyCost >= 0)
            {
                generatedEnemies.Add(enemies[randEnemyId].enemyType);
                waveValue -= randEnemyCost;
            }
            else if (waveValue <= 0)
            {
                break;
            }
        }

        enemiesToSpawn = generatedEnemies.Count;
        subWaves.Clear();
        subWaves = SplitWave(generatedEnemies);
    }

    private List<List<EnemyType>> SplitWave(List<EnemyType> enemiesToSpawn)
    {
        List<List<EnemyType>> subWaves = new List<List<EnemyType>>();
        int subWaveSize = Mathf.CeilToInt((float)enemiesToSpawn.Count / totalSubWaves);
        for (int i = 0; i < totalSubWaves; i++)
        {
            int startIndex = i * subWaveSize;
            int endIndex = Mathf.Min(startIndex + subWaveSize, enemiesToSpawn.Count);
            List<EnemyType> subWave = enemiesToSpawn.GetRange(startIndex, endIndex - startIndex);
            subWaves.Add(subWave);
        }
        return subWaves;
    }

    private int CalcNewValue()
    {
        int totalValues = 0;

        foreach (var enemy in enemies)
        {
            totalValues += enemy.cost;
        }

        return ((totalValues / enemies.Count) * minTotalEnemies);
    }

}