using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct EnemySpawnEntry
{
    [Tooltip("Configuração do inimigo (BasicEnemy, FastEnemy, TankEnemy, etc.)")]
    public EnemiesConfig config;

    [Tooltip("ID da pool correspondente no ObjectPool (opcional se usar Instantiate)")]
    public PoolID poolID;

    [Tooltip("Peso de spawn caso não queira calcular automaticamente pelo Cost")]
    [Range(1f, 100f)]
    public float customWeight;

    [Tooltip("Marque se este inimigo for raro para nunca spawnar agrupado")]
    public bool isRare;
}

public class WaveManager : MonoBehaviour
{
    [Header("Configurações da Wave")]
    [Tooltip("Quantidade total de inimigos na primera wave")]
    [SerializeField] private int initialWaveSize = 500;

    [Tooltip("Quantidade adicional de inimigos a cada nova wave")]
    [SerializeField] private int waveSizeIncrease = 100;

    [Tooltip("Quantidade de inimigos gerados por lote")]
    [SerializeField] private int batchSize = 50;

    [Header("Limites e Desempenho")]
    [Tooltip("Limite máximo de inimigos vivos na cena ao mesmo tempo")]
    [SerializeField] private int maxActiveEnemies = 120;

    [Tooltip("Intervalo em segundos entre cada grupo de 3 spawns dentro do lote")]
    [SerializeField] private float delayBetweenSpawns = 0.35f;

    [Tooltip("Intervalo em segundos entre um lote de 50 e o próximo")]
    [SerializeField] private float delayBetweenBatches = 4f;

    [Header("Pontos de Spawn")]
    [Tooltip("Todos os pontos de spawn disponíveis pelo mapa")]
    [SerializeField] private List<Transform> mapSpawnPoints = new List<Transform>();

    [Tooltip("Quantos pontos de spawn diferentes serão escolhidos por lote")]
    [SerializeField] private int activeSpawnsPerBatch = 3;

    [Header("Raridade e Balanceamento")]
    [Tooltip("Lista de inimigos que podem aparecer na wave")]
    [SerializeField] private List<EnemySpawnEntry> enemyEntries = new List<EnemySpawnEntry>();

    [Tooltip("Se verdadeiro, calcula a chance de spawn automaticamente usando 1 / cost do EnemiesConfig")]
    [SerializeField] private bool useInverseCostAsWeight = true;

    [Tooltip("Quantidade mínima de inimigos comuns que devem nascer entre dois inimigos raros")]
    [SerializeField] private int minSpacingBetweenRares = 8;

    private int currentWave = 0;
    private int totalEnemiesToSpawnInWave = 0;
    private int enemiesSpawnedInWave = 0;
    private int activeEnemiesCount = 0;
    private int spawnsSinceLastRare = 0;
    private bool isWaveActive = false;

    public static WaveManager Instance { get; private set; }

    private void Start()
    {
        Instance = this;
        StartNextWave();
    }

    /// <summary>
    /// Inicia a próxima wave aumentando o tamanho conforme configurado.
    /// </summary>
    public void StartNextWave()
    {
        if (isWaveActive) return;

        currentWave++;
        totalEnemiesToSpawnInWave = initialWaveSize + ((currentWave - 1) * waveSizeIncrease);
        enemiesSpawnedInWave = 0;
        spawnsSinceLastRare = minSpacingBetweenRares; // Permite um raro logo no início se sorteado
        isWaveActive = true;

        StartCoroutine(SpawnWaveRoutine());
    }

    private IEnumerator SpawnWaveRoutine()
    {
        while (enemiesSpawnedInWave < totalEnemiesToSpawnInWave)
        {
            // Espera até que o número de inimigos ativos caia abaixo do limite máximo
            yield return new WaitUntil(() => activeEnemiesCount < maxActiveEnemies);

            int remainingInWave = totalEnemiesToSpawnInWave - enemiesSpawnedInWave;
            int currentBatchCount = Mathf.Min(batchSize, remainingInWave);

            // Sorteia 3 pontos de spawn diferentes do mapa para este lote de 50
            List<Transform> selectedSpawns = GetRandomSpawnPoints(activeSpawnsPerBatch);

            // Gera a fila do lote garantindo que inimigos raros fiquem espaçados
            Queue<EnemySpawnEntry> batchQueue = GenerateSpacedBatch(currentBatchCount);

            int spawnPointIndex = 0;

            while (batchQueue.Count > 0)
            {
                // Se atingir o limite de 120 inimigos ativos no meio do lote, aguarda abrir vaga
                if (activeEnemiesCount >= maxActiveEnemies)
                {
                    yield return new WaitUntil(() => activeEnemiesCount < maxActiveEnemies);
                }

                EnemySpawnEntry entry = batchQueue.Dequeue();
                Transform targetSpawn = selectedSpawns[spawnPointIndex % selectedSpawns.Count];

                SpawnEnemy(entry, targetSpawn);

                enemiesSpawnedInWave++;
                spawnPointIndex++;

                // Aguarda um pequeno intervalo a cada rodada nos 3 spawns ativos
                if (spawnPointIndex % selectedSpawns.Count == 0)
                {
                    yield return new WaitForSeconds(delayBetweenSpawns);
                }
            }

            // Pausa estratégica antes de sortear os próximos 3 spawns e soltar mais 50 inimigos
            if (enemiesSpawnedInWave < totalEnemiesToSpawnInWave)
            {
                yield return new WaitForSeconds(delayBetweenBatches);
            }
        }

        // Aguarda todos os inimigos restantes morrerem para encerrar a wave
        yield return new WaitUntil(() => activeEnemiesCount <= 0);
        isWaveActive = false;
        Debug.Log($"Wave {currentWave} concluída!");
    }

    /// <summary>
    /// Gera a lista de inimigos do lote impedindo que raros nasçam juntos.
    /// </summary>
    private Queue<EnemySpawnEntry> GenerateSpacedBatch(int count)
    {
        Queue<EnemySpawnEntry> queue = new Queue<EnemySpawnEntry>(count);

        if (enemyEntries == null || enemyEntries.Count == 0)
        {
            return queue;
        }

        for (int i = 0; i < count; i++)
        {
            bool allowRare = spawnsSinceLastRare >= minSpacingBetweenRares;
            EnemySpawnEntry chosen = SelectWeightedEnemy(allowRare);

            queue.Enqueue(chosen);

            if (IsEnemyRare(chosen))
            {
                spawnsSinceLastRare = 0;
            }
            else
            {
                spawnsSinceLastRare++;
            }
        }

        return queue;
    }

    /// <summary>
    /// Sorteia um inimigo baseado no peso/raridade, filtrando raros se estiverem em cooldown.
    /// </summary>
    private EnemySpawnEntry SelectWeightedEnemy(bool allowRare)
    {
        if (enemyEntries == null || enemyEntries.Count == 0)
        {
            return default;
        }

        float totalWeight = 0f;

        foreach (EnemySpawnEntry entry in enemyEntries)
        {
            if (!allowRare && IsEnemyRare(entry)) continue;
            totalWeight += GetEntryWeight(entry);
        }

        // Fallback caso todos os inimigos cadastrados sejam marcados como raros
        if (totalWeight <= 0f)
        {
            return enemyEntries[Random.Range(0, enemyEntries.Count)];
        }

        float randomValue = Random.Range(0f, totalWeight);
        float cumulativeWeight = 0f;

        foreach (EnemySpawnEntry entry in enemyEntries)
        {
            if (!allowRare && IsEnemyRare(entry)) continue;

            cumulativeWeight += GetEntryWeight(entry);
            if (randomValue <= cumulativeWeight)
            {
                return entry;
            }
        }

        return enemyEntries[0];
    }

    private float GetEntryWeight(EnemySpawnEntry entry)
    {
        if (useInverseCostAsWeight && entry.config != null && entry.config.cost > 0)
        {
            // Usa 100 / cost: Basic (cost 1) = 100, Fast (cost 3) = 33.3, Tank (cost 20) = 5
            return 100f / entry.config.cost;
        }
        return entry.customWeight;
    }

    private bool IsEnemyRare(EnemySpawnEntry entry)
    {
        // Considera raro se estiver marcado no Inspector ou se o custo for alto (ex: TankEnemy com cost >= 10)
        return entry.isRare || (entry.config != null && entry.config.cost >= 10);
    }

    /// <summary>
    /// Escolhe N pontos de spawn únicos aleatoriamente pelo mapa.
    /// </summary>
    private List<Transform> GetRandomSpawnPoints(int amount)
    {
        List<Transform> poolCopy = new List<Transform>(mapSpawnPoints);
        List<Transform> result = new List<Transform>();

        int count = Mathf.Min(amount, poolCopy.Count);
        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, poolCopy.Count);
            result.Add(poolCopy[randomIndex]);
            poolCopy.RemoveAt(randomIndex);
        }

        return result;
    }

    private void SpawnEnemy(EnemySpawnEntry entry, Transform spawnPoint)
    {
        GameObject enemyObj = null;

        // Tenta puxar do ObjectPool primeiro; se não houver PoolID, instancia o prefab do EnemiesConfig
        if (entry.poolID != null && ObjectPool.Instance != null)
        {
            enemyObj = ObjectPool.Instance.GetInstance(entry.poolID);
        }
        else if (entry.config != null && entry.config.enemyPrefab != null)
        {
            enemyObj = Instantiate(entry.config.enemyPrefab);
        }

        if (enemyObj != null)
        {
            // Adiciona um pequeno deslocamento aleatório para não empilhar colisores no mesmo exato pixel
            Vector2 randomCircle = Random.insideUnitCircle * 10f;
            Vector3 spawnPosition = spawnPoint.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            enemyObj.transform.SetPositionAndRotation(spawnPosition, spawnPoint.rotation);
            enemyObj.SetActive(true);

            ShipManager.Instance.AddActiveShip(enemyObj.transform);

            activeEnemiesCount++;
        }
    }

    /// <summary>
    /// Chame este método no script de vida/morte do inimigo quando ele morrer e for desativado.
    /// </summary>
    public void RegisterEnemyDeath()
    {
        activeEnemiesCount = Mathf.Max(0, activeEnemiesCount - 1);
        ShipManager.Instance.RemoveDisabledShips();
    }
}