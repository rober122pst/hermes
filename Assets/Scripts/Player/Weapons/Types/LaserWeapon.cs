using System.Collections.Generic;
using Player;
using UnityEngine;

[DefaultExecutionOrder(200)]
public class LaserWeapon : WeaponBase
{
    private struct Shot
    {
        public GameObject projectile;
        public float lifetime;
        public float speed;
        public float radius;
        public float damage;
        public float knockback;
    }

    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private AudioClip shotClip;
    [SerializeField] private AudioClip impactLayerClip;
    [SerializeField, Min(0.1f)] private float sweepRadius = 1.15f;

    private const int CannonCount = 2;
    private readonly List<Transform> muzzles = new(CannonCount);
    private readonly List<Shot> shots = new(16);
    private readonly Collider[] overlaps = new Collider[16];
    private AudioSource shotAudio;
    private Boresight boresight;
    private NaveController pilot;
    private float nextShot;

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        boresight = player.GetComponent<Boresight>();
        pilot = player.GetComponent<NaveController>();

        foreach (Transform child in player.GetComponentsInChildren<Transform>())
        {
            if (child.CompareTag("LaserWeapon"))
                muzzles.Add(child);
        }

        if (shotClip != null)
        {
            shotAudio = gameObject.AddComponent<AudioSource>();
            shotAudio.playOnAwake = false;
            shotAudio.outputAudioMixerGroup = player.GetComponent<AudioSource>()?.outputAudioMixerGroup;
        }
    }

    private void LateUpdate()
    {
        if (weaponData == null || playerTransform == null)
            return;

        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f || Time.timeScale <= 0f || (pilot != null && !pilot.IsAlive))
        {
            boresight?.ClearAim();
            return;
        }

        ObjectPool pool = ObjectPool.Instance;
        nextShot -= deltaTime;
        bool firing = nextShot <= 0f && pool != null;
        if (shots.Count > 0 || firing)
            Physics.SyncTransforms();

        UpdateShots(deltaTime);

        float speed = Mathf.Max(weaponData.speed, 1f);
        float lifetime = Mathf.Max(weaponData.lifetime, 0.05f);
        Vector3 aimPoint = boresight != null
            ? boresight.ResolveAim(speed, lifetime, firing)
            : playerTransform.position + playerTransform.forward * speed * lifetime;

        if (!firing)
            return;

        FireSalvo(pool, aimPoint, speed, lifetime);
        float cooldown = Mathf.Max(0.06f, weaponData.attackCooldown);
        nextShot = Mathf.Max(0f, nextShot + cooldown);
    }

    private void FireSalvo(ObjectPool pool, Vector3 aimPoint, float speed, float lifetime)
    {
        float width = weaponData.projectileScale;
        for (int cannon = 0; cannon < CannonCount; cannon++)
        {
            GameObject projectile = pool.GetInstance(weaponData.projectilePoolID);
            if (projectile == null)
                return;

            Vector3 origin = cannon < muzzles.Count && muzzles[cannon] != null
                ? muzzles[cannon].position
                : playerTransform.TransformPoint(new Vector3(cannon == 0 ? -0.7f : 0.7f, 0f, 2f));
            Vector3 direction = aimPoint - origin;
            Quaternion rotation = direction.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(direction, playerTransform.up)
                : playerTransform.rotation;
            projectile.transform.SetPositionAndRotation(origin, rotation);
            projectile.transform.localScale = new Vector3(width, width, 1f);
            projectile.SetActive(true);

            shots.Add(new Shot
            {
                projectile = projectile,
                lifetime = lifetime,
                speed = speed,
                radius = sweepRadius * width,
                damage = weaponData.damage,
                knockback = level >= 8 ? 12f : 0f
            });
        }

        if (shotAudio != null)
        {
            bool hasTarget = boresight != null && boresight.HasTarget;
            shotAudio.pitch = Random.Range(0.9f, 1.1f);
            shotAudio.PlayOneShot(shotClip, hasTarget ? 0.25f : 0.14f);
            if (impactLayerClip != null)
                shotAudio.PlayOneShot(impactLayerClip, level >= 3 ? 0.10f : 0.05f);
        }

        if (boresight != null && boresight.HasTarget)
            CinemachineShake.Instance?.ShakeCamera(0.16f, 0.09f);
    }

    private void UpdateShots(float deltaTime)
    {
        for (int i = shots.Count - 1; i >= 0; i--)
        {
            Shot shot = shots[i];
            if (shot.projectile == null || !shot.projectile.activeSelf)
            {
                shots.RemoveAt(i);
                continue;
            }

            Transform projectile = shot.projectile.transform;
            float displacement = Mathf.Min(shot.lifetime, deltaTime) * shot.speed;
            Collider hit = FindHit(projectile.position, projectile.forward, shot.radius, displacement);
            if (hit != null)
            {
                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable is EnemySystem enemy && enemy.CanTakeDamage && shot.knockback > 0f)
                    enemy.ApplyKnockback(projectile.forward * shot.knockback);

                if (damageable != null)
                {
                    if (damageable is not EnemySystem shielded || shielded.CanTakeDamage)
                        boresight?.ConfirmImpact();
                    damageable.TakeDamage(shot.damage);
                }

                shot.projectile.SetActive(false);
                shots.RemoveAt(i);
                continue;
            }

            projectile.position += projectile.forward * displacement;
            shot.lifetime -= deltaTime;
            if (shot.lifetime <= 0f)
            {
                shot.projectile.SetActive(false);
                shots.RemoveAt(i);
            }
            else
            {
                shots[i] = shot;
            }
        }
    }

    private Collider FindHit(Vector3 origin, Vector3 direction, float radius, float distance)
    {
        // O SphereCast só encontra colisores ainda fora da esfera no começo do trecho.
        int count = Physics.OverlapSphereNonAlloc(origin, radius, overlaps, enemyLayer, QueryTriggerInteraction.Collide);
        Collider closest = null;
        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            Collider candidate = overlaps[i];
            float squaredDistance = (candidate.bounds.ClosestPoint(origin) - origin).sqrMagnitude;
            if (squaredDistance < closestDistance)
            {
                closest = candidate;
                closestDistance = squaredDistance;
            }
        }

        if (closest != null)
            return closest;

        return Physics.SphereCast(origin, radius, direction, out RaycastHit hit, distance, enemyLayer,
            QueryTriggerInteraction.Collide) ? hit.collider : null;
    }

    private void OnDisable()
    {
        foreach (Shot shot in shots)
        {
            if (shot.projectile != null)
                shot.projectile.SetActive(false);
        }

        shots.Clear();
        boresight?.ClearAim();
    }
}
