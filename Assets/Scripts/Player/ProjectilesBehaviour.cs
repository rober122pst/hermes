using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Projectiles/Targeting/NearestEnemy")]
public class NearestEnemyTargetingSO : ScriptableObject, ITargetingStrategy
{
    T FindNearestEnemy<T>(Vector3 playerTransform) where T : class
    {
        T[] candidates = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<T>()
            .ToArray();

        T nearestEnemy = null;
        float nearestDistance = float.MaxValue;

        foreach (T candidate in candidates)
        {
            float distance = Vector3.Distance(playerTransform, ((MonoBehaviour)(object)candidate).transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = candidate;
            }
        }

        return nearestEnemy;
    }

    public Vector3 GetTargetDirection(Transform playerTransform)
    {
        IDamageable nearestEnemy = FindNearestEnemy<IDamageable>(playerTransform.position);
        return nearestEnemy != null ? ((MonoBehaviour)nearestEnemy).transform.position - playerTransform.position : Vector3.forward;
    }
}

[CreateAssetMenu(menuName = "Projectiles/Movement/Orbital")]
public class OrbitalMovementSO : ScriptableObject, IMovementStrategy
{
    public float orbitRadius = 2f;

    public void Move(Transform projectile, Vector3 direction, float speed)
    {
        Vector3 orbitCenter = projectile.position - direction.normalized * orbitRadius;
        Vector3 orbitDirection = (projectile.position - orbitCenter).normalized;
        Vector3 tangentDirection = Vector3.Cross(orbitDirection, Vector3.up).normalized;
        projectile.position += tangentDirection * speed * Time.deltaTime;
    }
}

[CreateAssetMenu(menuName = "Projectiles/Targeting/Foward")]
public class ForwardTargetingSO : ScriptableObject, ITargetingStrategy
{
    public Vector3 GetTargetDirection(Transform playerTransform)
    {
        return playerTransform.forward;
    }
}