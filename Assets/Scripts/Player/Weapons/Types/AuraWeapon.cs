using System.Collections.Generic;
using UnityEngine;

public class AuraWeapon : WeaponBase
{
    public GameObject auraPrefab;
    public float baseAuraRadius = 10f;
    public LayerMask enemyLayer;

    private readonly Collider[] hits = new Collider[48];
    private readonly HashSet<IDamageable> damaged = new();
    private GameObject field;
    private Transform visual;
    private float cycleTime;
    private float nextPulse;

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        if (auraPrefab == null) return;

        field = Instantiate(auraPrefab, playerTransform);
        visual = field.transform.childCount > 0 ? field.transform.GetChild(0) : field.transform;
        field.SetActive(true);
    }

    private void Update()
    {
        if (field == null || Time.deltaTime <= 0f) return;

        float duration = Mathf.Max(0.1f, weaponData.lifetime);
        float cycle = duration + Mathf.Max(0f, weaponData.attackCooldown);
        cycleTime = Mathf.Repeat(cycleTime + Time.deltaTime, cycle);

        bool active = cycleTime < duration;
        if (field.activeSelf != active) field.SetActive(active);
        if (!active) return;

        float radius = weaponData.projectileScale * baseAuraRadius;
        float fade = Mathf.Min(Mathf.Clamp01(cycleTime / 0.18f), Mathf.Clamp01((duration - cycleTime) / 0.2f));
        visual.localScale = new Vector3(radius * 2f * fade, visual.localScale.y, radius * 2f * fade);
        if (Time.time < nextPulse || fade < 0.9f) return;

        nextPulse = Time.time + Mathf.Max(0.1f, weaponData.projectileCooldown);
        damaged.Clear();
        int count = Physics.OverlapSphereNonAlloc(playerTransform.position, radius, hits,
            enemyLayer, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            Collider hit = hits[i];
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || !damaged.Add(target)) continue;

            if (target is EnemySystem enemy)
            {
                Vector3 outward = (enemy.transform.position - playerTransform.position).normalized;
                enemy.ApplyKnockback(outward * (8f + (level - 1) * 1.6f));
            }
            target.TakeDamage(weaponData.damage);
        }
    }

    private void OnDisable()
    {
        if (field != null) field.SetActive(false);
    }

    protected override void OnDestroy()
    {
        if (field != null) Destroy(field);
        base.OnDestroy();
    }
}
