using UnityEngine;

public class EnemySpawnPool : ObjectPool
{
    public EnemiesConfig config;
    void Start()
    {
        prefab = config.enemyPrefab;
    }
}
