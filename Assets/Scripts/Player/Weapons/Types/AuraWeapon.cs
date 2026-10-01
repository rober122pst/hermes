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
    private float hitCooldown;
    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        SpawnProjectiles();
    }

    private void SpawnProjectiles()
    {
        GameObject p = Instantiate(auraPrefab, playerTransform);
        p.SetActive(true);
        activeProjectile = p;
        UpdateAuraSize();
    }

    private void Update()
    {
        AuraUpdate();
    }

    private void UpdateAuraSize()
    {
        if (activeProjectile == null) return;
        Transform auraObject = activeProjectile.transform.GetChild(0);
        auraObject.localScale = new Vector3(auraRadius * 2, auraObject.localScale.y, auraRadius * 2);
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

        if (hitCooldown <= 0f)
        {
            foreach (RaycastHit hit in hits)
            {
                Collider hitCollider = hit.collider;

                // Verifica se o alvo ja saiu do cooldown
                // Registra o tempo do hit
                hitCooldown = weaponData.projectileCooldown;

                IDamageable target = hitCollider.GetComponentInParent<IDamageable>();
                if (target != null)
                {
                    target.TakeDamage(weaponData.damage);
                    Debug.Log("Alvo detetado e levou dano: " + hitCollider.name);
                }
            }
        }

        hitCooldown -= Time.deltaTime;
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