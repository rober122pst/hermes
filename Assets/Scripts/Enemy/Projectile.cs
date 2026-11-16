using System.Collections.Generic;
using Player;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private struct BulletFlight
    {
        public GameObject Bullet;
        public Transform Target;
        public IDamageable Recipient;
        public Vector3 PreviousTargetPosition;
        public float Damage;
        public float RemainingTime;
        public float Speed;
        public float TurnSpeed;
        public float HitRadius;
    }

    public static Projectile Instance { get; private set; }

    [Min(0.1f)] public float lifetime = 2f;
    [Min(0.1f)] public float speed = 40f;

    private readonly List<BulletFlight> activeBullets = new List<BulletFlight>();

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void AddBullet(EnemyBullet bullet, Transform target, IDamageable recipient, float damage,
        float shotSpeed, float shotRange, float turnSpeed, float hitRadius)
    {
        if (shotSpeed <= 0f) shotSpeed = speed;
        activeBullets.Add(new BulletFlight
        {
            Bullet = bullet.gameObject,
            Target = target,
            Recipient = recipient,
            PreviousTargetPosition = target.position,
            Damage = damage,
            RemainingTime = Mathf.Max(lifetime, (shotRange + 12f) / shotSpeed),
            Speed = shotSpeed,
            TurnSpeed = turnSpeed,
            HitRadius = hitRadius
        });
    }

    public void RemoveBullet(GameObject bullet)
    {
        for (int i = activeBullets.Count - 1; i >= 0; i--)
        {
            if (activeBullets[i].Bullet != bullet) continue;

            activeBullets.RemoveAt(i);
            return;
        }
    }

    public void Clear()
    {
        for (int i = activeBullets.Count - 1; i >= 0; i--)
            if (activeBullets[i].Bullet != null) activeBullets[i].Bullet.SetActive(false);

        activeBullets.Clear();
    }

    private void Update()
    {
        float frameTime = Time.deltaTime;
        if (frameTime <= 0f) return;

        for (int i = activeBullets.Count - 1; i >= 0; i--)
        {
            BulletFlight flight = activeBullets[i];
            if (flight.Bullet == null || !flight.Bullet.activeInHierarchy)
            {
                activeBullets.RemoveAt(i);
                continue;
            }

            float step = Mathf.Min(frameTime, flight.RemainingTime);
            Transform bulletTransform = flight.Bullet.transform;
            Vector3 origin = bulletTransform.position;
            Vector3 direction = bulletTransform.forward;

            if (flight.Target != null && flight.TurnSpeed > 0f)
            {
                Vector3 toTarget = flight.Target.position - origin;
                if (toTarget.sqrMagnitude > 0.001f)
                    direction = Vector3.RotateTowards(direction, toTarget.normalized, flight.TurnSpeed * Mathf.Deg2Rad * step, 0f);
            }

            Vector3 nextPosition = origin + direction * (flight.Speed * step);
            bulletTransform.SetPositionAndRotation(nextPosition, Quaternion.LookRotation(direction));

            bool hit = false;
            if (flight.Target != null && flight.Recipient != null)
            {
                Vector3 targetPosition = Vector3.Lerp(flight.PreviousTargetPosition, flight.Target.position, step / frameTime);
                hit = CrossesPlayer(origin - flight.PreviousTargetPosition, nextPosition - targetPosition, flight.HitRadius);
                flight.PreviousTargetPosition = targetPosition;
            }

            flight.RemainingTime -= step;
            if (hit || flight.RemainingTime <= 0f)
            {
                activeBullets.RemoveAt(i);
                flight.Bullet.SetActive(false);
                if (hit && flight.Recipient is NaveController pilot)
                    pilot.TakeDamage(flight.Damage, flight.Target.position - direction * 12f);
                else if (hit)
                    flight.Recipient.TakeDamage(flight.Damage);
            }
            else
            {
                activeBullets[i] = flight;
            }
        }
    }

    // Os dois pontos estão no referencial do jogador para também capturar movimentos rápidos da nave.
    internal static bool CrossesPlayer(Vector3 start, Vector3 end, float hitRadius)
    {
        Vector3 movement = end - start;
        float lengthSquared = movement.sqrMagnitude;
        float closest = lengthSquared > 0.000001f
            ? Mathf.Clamp01(-Vector3.Dot(start, movement) / lengthSquared)
            : 0f;
        return (start + closest * movement).sqrMagnitude <= hitRadius * hitRadius;
    }
}
