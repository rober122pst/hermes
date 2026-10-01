using UnityEngine;

public class EnemyBullet : MonoBehaviour, IDamageable
{
    public void TakeDamage(float damage)
    {
        Debug.Log($"EnemyBullet took {damage} damage.");
        Projectile.Instance.RemoveBullet(gameObject);
        gameObject.SetActive(false);
    }
}
