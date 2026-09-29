using System.Collections.Generic;
using UnityEngine;

public class AuraWeapon : WeaponBase
{
    public GameObject auraPrefab;
    private GameObject activeProjectile;

    [Header("Configuracoes da Colisao")]
    public float auraRadius = 10f;
    public float castDistance = 0.1f;
    public LayerMask enemyLayer;

    // Variaveis para o controle de cooldown
    private Dictionary<Collider, float> hitCooldowns = new Dictionary<Collider, float>();
    private List<Collider> collidersToRemove = new List<Collider>();

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        SpawnProjectiles();
    }

    private void SpawnProjectiles()
    {
        for (int i = 0; i < weaponData.projectileAmount; i++)
        {
            GameObject p = Instantiate(auraPrefab, playerTransform);
            p.SetActive(true);
            activeProjectile = p;
        }
    }

    private void Update()
    {
        // Limpa a memoria de cooldowns continuamente
        CleanUpCooldowns();
        AuraUpdate();
    }

    private void AuraUpdate()
    {
        if (activeProjectile == null) return;

        // Ponto de origem do cast
        Vector3 origin = activeProjectile.transform.position;
        // Direcao do cast
        Vector3 direction = activeProjectile.transform.forward;

        // Dispara o SphereCastAll para capturar todos os colisores na area
        RaycastHit[] hits = Physics.SphereCastAll(origin, auraRadius, direction, castDistance, enemyLayer);

        foreach (RaycastHit hit in hits)
        {
            Collider hitCollider = hit.collider;

            // Verifica se o alvo ja saiu do cooldown
            if (CanHitTarget(hitCollider))
            {
                // Registra o tempo do hit
                hitCooldowns[hitCollider] = Time.time;

                IDamageable target = hitCollider.GetComponentInParent<IDamageable>();
                if (target != null)
                {
                    target.TakeDamage(weaponData.damage);
                    Debug.Log("Alvo detetado e levou dano: " + hitCollider.name);
                }
            }
        }
    }

    private bool CanHitTarget(Collider target)
    {
        if (!hitCooldowns.ContainsKey(target))
        {
            return true;
        }
        return Time.time >= hitCooldowns[target] + weaponData.projectileCooldown;
    }

    private void CleanUpCooldowns()
    {
        collidersToRemove.Clear();

        foreach (var kvp in hitCooldowns)
        {
            if (kvp.Key == null || !kvp.Key.gameObject.activeInHierarchy || Time.time >= kvp.Value + weaponData.projectileCooldown)
            {
                collidersToRemove.Add(kvp.Key);
            }
        }

        for (int i = 0; i < collidersToRemove.Count; i++)
        {
            hitCooldowns.Remove(collidersToRemove[i]);
        }
    }

    // Metodo para desenhar a area da aura no editor, facilitando o ajuste do raio
    private void OnDrawGizmosSelected()
    {
        if (activeProjectile != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(activeProjectile.transform.position, auraRadius);
        }
    }
}