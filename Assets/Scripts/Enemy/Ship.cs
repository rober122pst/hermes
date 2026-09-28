using UnityEngine;
using System.Collections.Generic;

namespace Enemy
{
    public class Ship : MonoBehaviour
    {
        [Header("Chase Settings")]
        [Tooltip("Arraste o Transform do jogador aqui")]
        public Transform player;

        public float speed = 5f;
        public float stopDistance = 10f;
        public float rotationSpeed = 5f;

        [Tooltip("Se TRUE: Anda de lado olhando pro jogador. Se FALSE: Olha para a direção do movimento.")]
        public bool alwaysLookAtPlayer = false;

        [Header("Evasion Settings")]
        [Tooltip("O quão perto outro inimigo precisa estar para ele começar a desviar")]
        public float evasionRadius = 4f;
        [Tooltip("A força com que as naves se repelem")]
        public float evasionForce = 10f;

        // Variáveis gerenciadas invisivelmente pelo Manager
        [HideInInspector] public Vector3Int currentPartition;
        [HideInInspector] public int batchId;

        // Guardamos o movimento de evasão calculado no turno do batch
        private Vector3 evasionMovement = Vector3.zero;

        private void Start()
        {
            // Registra a nave no sistema ao nascer
            if (Manager.Instance != null)
            {
                Manager.Instance.AddEnemy(this);
            }
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        private void OnDestroy()
        {
            // Limpa a memória corretamente ao morrer
            if (Manager.Instance != null)
            {
                Manager.Instance.RemoveEnemy(this);
            }
        }

        private void Update()
        {
            if (player == null) return;

            // Atualiza no Grid em qual quadrante virtual estamos
            if (Manager.Instance != null)
            {
                Manager.Instance.UpdateEnemyPartition(this);
            }

            Vector3 movementDirection = Vector3.zero;
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            // 1. Move na direção do jogador se estiver longe
            if (distanceToPlayer > stopDistance)
            {
                movementDirection = (player.position - transform.position).normalized * speed;
            }

            // 2. Soma a força de evasão (calculada de forma parcelada pelo Manager)
            movementDirection += evasionMovement * evasionForce;

            // 3. Aplica o movimento real no espaço 3D (X, Y, Z)
            transform.position += movementDirection * Time.deltaTime;

            // 4. Aplica a Rotação
            Rotate(movementDirection);
        }

        // Método chamado pelo Manager APENAS no turno específico desta nave
        public void CalculateEvasion()
        {
            evasionMovement = Vector3.zero;
            if (Manager.Instance == null) return;

            // Pega inimigos nas partições próximas SEM usar a pesada Física do Unity
            List<Ship> nearbyEnemies = Manager.Instance.GetNearbyEnemies(currentPartition);
            int closeEnemiesCount = 0;

            float evasionRadiusSqr = evasionRadius * evasionRadius; // Otimização para evitar raiz quadrada

            for (int i = 0; i < nearbyEnemies.Count; i++)
            {
                Ship other = nearbyEnemies[i];
                if (other != this && other != null)
                {
                    Vector3 difference = transform.position - other.transform.position;

                    // sqrMagnitude é incrivelmente mais rápido que Vector3.Distance
                    float sqrDistance = difference.sqrMagnitude;

                    // Se estiver dentro do raio e não estiver em colisão exata no mesmo milímetro
                    if (sqrDistance < evasionRadiusSqr && sqrDistance > 0.001f)
                    {
                        // MEGA OTIMIZAÇÃO: difference / sqrDistance aplica repulsão baseada em proximidade
                        // sem precisar usar Mathf.Sqrt() ou Normalize()! 
                        evasionMovement += difference / sqrDistance;
                        closeEnemiesCount++;
                    }
                }
            }

            if (closeEnemiesCount > 0)
            {
                evasionMovement /= closeEnemiesCount;
            }
        }

        private void Rotate(Vector3 movementDirection)
        {
            Vector3 lookDirection = movementDirection;

            // Olha para o jogador se a flag estiver ativa, ou se estiver parado
            if (alwaysLookAtPlayer || movementDirection.magnitude < 0.1f)
            {
                lookDirection = player.position - transform.position;
            }

            // Rotação suave usando Slerp
            if (lookDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
}