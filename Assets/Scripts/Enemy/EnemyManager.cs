using System;
using System.Collections.Generic;
using Enemy;
using UnityEngine;

namespace Game.Enemy
{
    // Armazena a pontuação de cada grupo para manter a distribuição equilibrada.
    public class BatchScore : IComparable<BatchScore>
    {
        public int batchId;
        public int enemyCount;

        public int CompareTo(BatchScore other)
        {
            // Prioriza o grupo com menos inimigos.
            int result = enemyCount.CompareTo(other.enemyCount);
            if (result == 0)
            {
                // Usa o identificador para desempatar e evitar itens iguais no SortedSet.
                return batchId.CompareTo(other.batchId);
            }
            return result;
        }
    }

    public class EnemyManager : MonoBehaviour
    {
        public static EnemyManager Instance;

        [Header("Spatial Partitioning")]
        [Tooltip("Size of each cell in the virtual grid")]
        public float partitionSize = 5f;

        [Header("Time Slicing")]
        [Tooltip("How many frames for a full evasion calculation cycle")]
        public int totalBatches = 50;

        // Grade espacial que substitui Physics.OverlapSphere.
        private Dictionary<Vector3Int, List<Ship>> spatialGrid;

        // Grupos de processamento dos inimigos.
        private List<Ship>[] enemyBatches;
        private SortedSet<BatchScore> batchScores;
        private BatchScore[] batchScoreRefs;

        private int currentBatchIndex = 0;

        // Lista reutilizada para reduzir alocações de memória.
        private List<Ship> queryBuffer;

        private void Awake()
        {
            Instance = this;
            spatialGrid = new Dictionary<Vector3Int, List<Ship>>();
            queryBuffer = new List<Ship>(500);

            enemyBatches = new List<Ship>[totalBatches];
            batchScores = new SortedSet<BatchScore>();
            batchScoreRefs = new BatchScore[totalBatches];

            // Inicializa os grupos de processamento.
            for (int i = 0; i < totalBatches; i++)
            {
                enemyBatches[i] = new List<Ship>();
                BatchScore bs = new BatchScore { batchId = i, enemyCount = 0 };
                batchScores.Add(bs);
                batchScoreRefs[i] = bs;
            }
        }

        // Converte uma posição do mundo em coordenadas da grade (X, Y, Z).
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
            // 1. Adiciona o inimigo à grade espacial.
            Vector3Int key = GetPartitionKey(enemy.transform.position);
            if (!spatialGrid.ContainsKey(key))
                spatialGrid[key] = new List<Ship>();

            spatialGrid[key].Add(enemy);
            enemy.currentPartition = key;

            // 2. Adiciona o inimigo ao grupo com menos integrantes - O(log N).
            BatchScore bestBatch = batchScores.Min;
            batchScores.Remove(bestBatch);
            bestBatch.enemyCount++;
            batchScores.Add(bestBatch); // O SortedSet reordena os grupos automaticamente.

            int bId = bestBatch.batchId;
            enemyBatches[bId].Add(enemy);
            enemy.batchId = bId;
        }

        public void RemoveEnemy(Ship enemy)
        {
            // Remove o inimigo da grade.
            if (spatialGrid.ContainsKey(enemy.currentPartition))
            {
                spatialGrid[enemy.currentPartition].Remove(enemy);
            }

            // Remove o inimigo do grupo correspondente e atualiza a pontuação.
            BatchScore bs = batchScoreRefs[enemy.batchId];
            batchScores.Remove(bs);
            bs.enemyCount--;
            batchScores.Add(bs);

            enemyBatches[enemy.batchId].Remove(enemy);
        }

        // Chamado pelo próprio inimigo quando ele se movimenta.
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
            // Executa o cálculo de evasão de apenas um grupo por quadro.
            List<Ship> currentBatch = enemyBatches[currentBatchIndex];
            for (int i = 0; i < currentBatch.Count; i++)
            {
                currentBatch[i].CalculateEvasion();
            }

            // Avança para o próximo grupo no quadro seguinte.
            currentBatchIndex = (currentBatchIndex + 1) % totalBatches;
        }

        // Retorna os inimigos da vizinhança 3D (3x3x3) sem consultar a física do Unity.
        public List<Ship> GetNearbyEnemies(Vector3Int centerPartition)
        {
            queryBuffer.Clear(); // Reutiliza o buffer para evitar novas alocações.

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
