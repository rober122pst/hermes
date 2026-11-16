using System.Collections.Generic;
using UnityEngine;

public class OrbitalWeapon : WeaponBase
{
    [Header("Configurações orbitais")]
    public float baseOrbitRadius = 10f;
    public float hitRadius = 0.65f;
    public LayerMask enemyLayer;
    public float scaleTransitionTime = 0.3f;
    public Vector3 maxProjectileScale = Vector3.one;

    private readonly List<GameObject> probes = new();
    private readonly RaycastHit[] sweptHits = new RaycastHit[24];
    private readonly Collider[] overlapHits = new Collider[24];
    private float angle;
    private float elapsed;
    private bool active = true;

    private void Update()
    {
        if (playerTransform == null || ObjectPool.Instance == null || Time.deltaTime <= 0f) return;

        angle = Mathf.Repeat(angle + weaponData.speed * Time.deltaTime, 360f);
        elapsed += Time.deltaTime;
        float duration = active ? weaponData.lifetime : weaponData.attackCooldown;
        if (elapsed >= Mathf.Max(0.05f, duration))
        {
            active = !active;
            elapsed = 0f;
            if (!active) ReleaseProbes();
        }

        if (!active) return;

        bool rebuild = probes.Count != weaponData.projectileAmount;
        if (rebuild) ReleaseProbes();
        CleanUpCooldowns();

        float fadeDuration = Mathf.Max(0.01f, Mathf.Min(scaleTransitionTime, weaponData.lifetime * 0.5f));
        float fade = Mathf.Min(Mathf.Clamp01(elapsed / fadeDuration),
            Mathf.Clamp01((weaponData.lifetime - elapsed) / fadeDuration));
        float pulse = 1f + Mathf.Sin(Time.time * 2.5f) * 0.12f;
        float radius = baseOrbitRadius * weaponData.projectileScale * pulse;
        int count = weaponData.projectileAmount;
        for (int i = 0; i < count; i++)
        {
            float radians = (angle + (360f / count) * i) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * radius;
            Vector3 position = playerTransform.TransformPoint(offset);

            GameObject probe = i < probes.Count ? probes[i] : GetProbe(position);
            if (probe == null) break;

            Vector3 previous = probe.transform.position;
            probe.transform.position = position;
            probe.transform.localScale = maxProjectileScale * fade;
            if (fade >= 0.25f) HitEnemies(previous, position, hitRadius * fade);
        }
    }

    private GameObject GetProbe(Vector3 position)
    {
        GameObject probe = ObjectPool.Instance.GetInstance(weaponData.projectilePoolID);
        if (probe == null) return null;

        probe.transform.position = position;
        probe.transform.localScale = Vector3.zero;
        probes.Add(probe);
        probe.SetActive(true);
        return probe;
    }

    private void HitEnemies(Vector3 previous, Vector3 position, float radius)
    {
        Vector3 travel = position - previous;
        float distance = travel.magnitude;
        if (distance > 0.001f)
        {
            int hits = Physics.SphereCastNonAlloc(previous, radius, travel / distance, sweptHits, distance,
                enemyLayer, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hits; i++) Damage(sweptHits[i].collider);
        }

        int overlaps = Physics.OverlapSphereNonAlloc(position, radius, overlapHits, enemyLayer,
            QueryTriggerInteraction.Collide);
        for (int i = 0; i < overlaps; i++) Damage(overlapHits[i]);
    }

    private void Damage(Collider collider)
    {
        if (collider == null || !collider.gameObject.activeInHierarchy || !CanHitTarget(collider)) return;

        IDamageable target = collider.GetComponentInParent<IDamageable>();
        if (target == null) return;
        hitCooldowns[collider] = Time.time;
        target.TakeDamage(weaponData.damage);
    }

    private void ReleaseProbes()
    {
        foreach (GameObject probe in probes)
        {
            if (probe != null) probe.SetActive(false);
        }
        probes.Clear();
    }

    private void OnDisable()
    {
        ReleaseProbes();
    }
}
