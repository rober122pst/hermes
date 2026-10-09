using System.Collections.Generic;
using UnityEngine;

public class OrbitalWeapon : WeaponBase
{
    [Header("Configurações Orbitais")]
    public float orbitRadius = 10f;

    [Header("Configurações de Colisão")]
    public float hitRadius = 0.5f;
    public LayerMask enemyLayer;

    [Header("Configurações de Ciclo (Escala)")]
    public float scaleTransitionTime = 0.3f; // Tempo para a animação de aumentar/diminuir
    public Vector3 maxProjectileScale = Vector3.one; // Escala máxima que o projétil alcançará

    private float currentAngle = 0f;
    private List<GameObject> activeProjectiles = new List<GameObject>();
    private RaycastHit[] hitResults = new RaycastHit[10];

    // Variáveis de controle de ciclo (Ativo / Inativo)
    private float cycleTimer = 0f;
    private bool isActivePhase = true;
    private float cooldown;

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        SpawnProjectiles();
    }

    private void SpawnProjectiles()
    {
        for (int i = 0; i < weaponData.projectileAmount; i++)
        {
            GameObject p = ObjectPool.Instance.GetInstance(weaponData.projectilePoolID);
            p.transform.localScale = Vector3.zero; // Começa invisível/pequeno
            p.SetActive(true);
            cooldown = weaponData.lifetime;
            activeProjectiles.Add(p);
        }
    }

    void Update()
    {
        // O ângulo de rotação continua girando sempre, mesmo na fase inativa
        currentAngle += weaponData.speed * Time.deltaTime;

        // Limpa a memória de cooldowns continuamente
        CleanUpCooldowns();

        // Gerencia os estados de Crescer, Ficar, Diminuir e Desativar
        UpdateCycle();
    }

    private void UpdateCycle()
    {
        // Prevenção de divisão por zero: se não houver cooldown, fica sempre ativo
        if (weaponData.attackCooldown <= 0.05f)
        {
            UpdateScale(1f);
            UpdateOrbitsAndCheckCollisions();
            return;
        }

        cycleTimer += Time.deltaTime;

        if (isActivePhase)
        {
            // Se o tempo ativo acabou, muda para a fase inativa
            if (cycleTimer >= cooldown)
            {
                isActivePhase = false;
                cooldown = weaponData.attackCooldown;
                cycleTimer = 0f;
                SetProjectilesActive(false); // Otimização: Desativa renderizadores/scripts
            }
            else
            {
                UpdateScaleBasedOnTimer();
                UpdateOrbitsAndCheckCollisions();
            }
        }
        else
        {
            // Fase inativa (dura o mesmo tempo que attackCooldown)
            if (cycleTimer >= cooldown)
            {
                isActivePhase = true;
                cycleTimer = 0f;
                cooldown = weaponData.lifetime;
                SetProjectilesActive(true);

                ForceUpdatePositions();
            }
        }
    }

    private void UpdateScaleBasedOnTimer()
    {
        // Garante que o tempo de crescer/diminuir não seja maior que a metade do tempo total ativo
        float clampedTransition = Mathf.Min(scaleTransitionTime, cooldown / 2f);
        float scaleMultiplier = 1f;

        if (cycleTimer < clampedTransition)
        {
            // Fase: Crescendo (0 até 1)
            scaleMultiplier = cycleTimer / clampedTransition;
        }
        else if (cycleTimer > cooldown - clampedTransition)
        {
            // Fase: Diminuindo (1 até 0)
            float shrinkTimer = cycleTimer - (cooldown - clampedTransition);
            scaleMultiplier = 1f - (shrinkTimer / clampedTransition);
        }

        UpdateScale(scaleMultiplier);
    }

    private void UpdateScale(float multiplier)
    {
        Vector3 newScale = maxProjectileScale * multiplier;
        for (int i = 0; i < activeProjectiles.Count; i++)
        {
            if (activeProjectiles[i] != null)
            {
                activeProjectiles[i].transform.localScale = newScale;
            }
        }
    }

    private void SetProjectilesActive(bool state)
    {
        for (int i = 0; i < activeProjectiles.Count; i++)
        {
            if (activeProjectiles[i] != null)
            {
                activeProjectiles[i].SetActive(state);
            }
        }
    }

    private void ForceUpdatePositions()
    {
        for (int i = 0; i < activeProjectiles.Count; i++)
        {
            if (activeProjectiles[i] == null) continue;

            float angle = currentAngle + 360f / weaponData.projectileAmount * i;
            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * orbitRadius;
            float z = Mathf.Sin(angle * Mathf.Deg2Rad) * orbitRadius;

            Vector3 localOffset = new Vector3(x, 0f, z);
            activeProjectiles[i].transform.position = playerTransform.position + (playerTransform.rotation * localOffset);
        }
    }

    private void UpdateOrbitsAndCheckCollisions()
    {
        for (int i = 0; i < activeProjectiles.Count; i++)
        {
            if (activeProjectiles[i] == null || !activeProjectiles[i].activeInHierarchy) continue;

            float angle = currentAngle + 360f / weaponData.projectileAmount * i;
            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * orbitRadius;
            float z = Mathf.Sin(angle * Mathf.Deg2Rad) * orbitRadius;

            Vector3 localOffset = new Vector3(x, 0f, z);
            Vector3 newPosition = playerTransform.position + (playerTransform.rotation * localOffset);
            Vector3 currentPosition = activeProjectiles[i].transform.position;

            Vector3 direction = newPosition - currentPosition;
            float distance = direction.magnitude;

            if (distance > 0.001f)
            {
                int hitCount = Physics.SphereCastNonAlloc(currentPosition, hitRadius, direction.normalized, hitResults, distance, enemyLayer);

                for (int j = 0; j < hitCount; j++)
                {
                    Collider hitCollider = hitResults[j].collider;

                    if (CanHitTarget(hitCollider))
                    {
                        hitCooldowns[hitCollider] = Time.time;

                        IDamageable target = hitCollider.GetComponentInParent<IDamageable>();
                        target.TakeDamage(weaponData.damage);
                    }
                }
            }

            activeProjectiles[i].transform.position = newPosition;
        }
    }



    private void OnDrawGizmosSelected()
    {
        if (activeProjectiles == null) return;

        Gizmos.color = Color.red;
        foreach (var p in activeProjectiles)
        {
            if (p != null && p.activeInHierarchy)
            {
                Gizmos.DrawWireSphere(p.transform.position, hitRadius);
            }
        }
    }
}