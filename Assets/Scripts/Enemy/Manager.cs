using System;
using System.Collections.Generic;
using UnityEngine;

// Classe que guarda a pontuação de cada grupo para balanceamento, como mostrado no vídeo
public class BatchScore : IComparable<BatchScore>
{
    public int batchId;
    public int enemyCount;

    public int CompareTo(BatchScore other)
    {
        // Ordena pelo menor número de inimigos
        int result = enemyCount.CompareTo(other.enemyCount);
        if (result == 0)
        {
            // Se empatar na quantidade, usa o ID para desempatar (evita que o SortedSet os exclua)
            return batchId.CompareTo(other.batchId);
        }
        return result;
    }
}

namespace Enemy
{
    public class Manager : MonoBehaviour
    {
        public static Manager Instance;

        [Header("Spatial Partitioning")]
        [Tooltip("Tamanho de cada cubo do grid virtual (ex: 5 unidades)")]
        public float partitionSize = 5f;

        [Header("Time Slicing")]
        [Tooltip("Quantos quadros (frames) demorará para um ciclo completo de cálculo de evasão")]
        public int totalBatches = 50;

        // Grid Espacial para substituir o Physics.OverlapSphere
        private Dictionary<Vector3Int, List<Ship>> spatialGrid;

        // Sistema de Batches (Grupos)
        private List<Ship>[] enemyBatches;
        private SortedSet<BatchScore> batchScores;
        private BatchScore[] batchScoreRefs;

        private int currentBatchIndex = 0;

        // Lista reutilizável para evitar Garbage Collection na memória
        private List<Ship> queryBuffer;

        private void Awake()
        {
            Instance = this;
            spatialGrid = new Dictionary<Vector3Int, List<Ship>>();
            queryBuffer = new List<Ship>(500);

            enemyBatches = new List<Ship>[totalBatches];
            batchScores = new SortedSet<BatchScore>();
            batchScoreRefs = new BatchScore[totalBatches];

            // Inicializa os grupos
            for (int i = 0; i < totalBatches; i++)
            {
                enemyBatches[i] = new List<Ship>();
                BatchScore bs = new BatchScore { batchId = i, enemyCount = 0 };
                batchScores.Add(bs);
                batchScoreRefs[i] = bs;
            }
        }

        // Transforma a posição 3D do mundo real em uma coordenada (X, Y, Z) do Grid
        public Vector3Int GetPartitionKey(Vector3 position)
        {
            return new Vector3Int(
                Mathf.FloorToInt(position.x / partitionSize),
                Mathf.FloorToInt(position.y / partitionSize),
                Mathf.FloorToInt(position.z / partitionSize)
            );
        }

        public void AddEnemy(Ship enemy)
        {
            // 1. Adiciona no Grid Espacial
            Vector3Int key = GetPartitionKey(enemy.transform.position);
            if (!spatialGrid.ContainsKey(key))
                spatialGrid[key] = new List<Ship>();

            spatialGrid[key].Add(enemy);
            enemy.currentPartition = key;

            // 2. Adiciona no melhor Batch (o que tem menos inimigos no momento) - O(log N)
            BatchScore bestBatch = batchScores.Min;
            batchScores.Remove(bestBatch);
            bestBatch.enemyCount++;
            batchScores.Add(bestBatch); // Reordena automaticamente no SortedSet

            int bId = bestBatch.batchId;
            enemyBatches[bId].Add(enemy);
            enemy.batchId = bId;
        }

        public void RemoveEnemy(Ship enemy)
        {
            // Remove do Grid
            if (spatialGrid.ContainsKey(enemy.currentPartition))
            {
                spatialGrid[enemy.currentPartition].Remove(enemy);
            }

            // Remove do Batch correspondente e atualiza a pontuação
            BatchScore bs = batchScoreRefs[enemy.batchId];
            batchScores.Remove(bs);
            bs.enemyCount--;
            batchScores.Add(bs);

            enemyBatches[enemy.batchId].Remove(enemy);
        }

        // Chamado pelo próprio inimigo quando ele anda
        public void UpdateEnemyPartition(Ship enemy)
        {
            Vector3Int newKey = GetPartitionKey(enemy.transform.position);
            if (newKey != enemy.currentPartition)
            {
                if (spatialGrid.ContainsKey(enemy.currentPartition))
                    spatialGrid[enemy.currentPartition].Remove(enemy);

                if (!spatialGrid.ContainsKey(newKey))
                    spatialGrid[newKey] = new List<Ship>();

                spatialGrid[newKey].Add(enemy);
                enemy.currentPartition = newKey;
            }
        }

        private void Update()
        {
            // Executa a lógica pesada de apenas 1 grupo por frame! (O segredo do vídeo)
            List<Ship> currentBatch = enemyBatches[currentBatchIndex];
            for (int i = 0; i < currentBatch.Count; i++)
            {
                currentBatch[i].CalculateEvasion();
            }

            // Avança para o próximo grupo no próximo frame
            currentBatchIndex = (currentBatchIndex + 1) % totalBatches;
        }

        // Retorna inimigos apenas da vizinhança 3D (3x3x3), sem usar Física do Unity
        public List<Ship> GetNearbyEnemies(Vector3Int centerPartition)
        {
            queryBuffer.Clear(); // Usa buffer para 0 alocações

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        Vector3Int neighborKey = new Vector3Int(
                            centerPartition.x + x,
                            centerPartition.y + y,
                            centerPartition.z + z
                        );

                        if (spatialGrid.TryGetValue(neighborKey, out List<Ship> enemiesInPartition))
                        {
                            queryBuffer.AddRange(enemiesInPartition);
                        }
                    }
                }
            }
            return queryBuffer;
        }
    }
}