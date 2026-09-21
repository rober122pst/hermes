using UnityEngine;

[CreateAssetMenu(fileName = "WeaponBehaviours", menuName = "Weapon/Base")]
public class WeaponBase : ScriptableObject
{
    [Header("Configurações da Arma")]
    public float attackCooldown = 2f;
    public float projectileCooldown = 0.2f;
    public int projectileAmount = 2;

    public virtual void Attack(float damage, GameObject projectile, Transform player)
    {
        Debug.Log("Movendo.");
    }

    public virtual void WeaponBehaviour(PoolID poolID) { }
}

[CreateAssetMenu(fileName = "WeaponBehaviours", menuName = "Weapon/Foward")]
public class Foward : WeaponBase
{
    public override void Attack(float damage, GameObject projectile, Transform player)
    {
        projectile.transform.position += projectile.transform.forward * 2f;
    }
}

[CreateAssetMenu(fileName = "WeaponBehaviours", menuName = "Weapon/Orbital")]
public class Orbital : WeaponBase
{
    [Header("Projectile Config")]
    public float orbitRadius = 10f;
    public float speed = 5f;

    float currentAngle = 0f;
    public override void WeaponBehaviour(PoolID poolID)
    {
        currentAngle += speed * Time.deltaTime;

        for (int i = 0; i < projectileAmount; i++)
        {
            GameObject proj = ObjectPool.Instance.GetInstance(poolID);
            Projectile p = proj.GetComponent<Projectile>() ? proj.GetComponent<Projectile>() : proj.AddComponent<Projectile>();
            p.SetWeaponConfig(this);
            proj.SetActive(true);
        }
        Debug.Log("Atacou");
    }

    public override void Attack(float damage, GameObject projectile, Transform player)
    {
        for (int i = 0; i < projectileAmount; i++)
        {
            if (projectile == null || player == null) return;

            float angle = currentAngle + (360f / projectileAmount) * i;

            float x = Mathf.Cos(angle) * orbitRadius;
            float z = Mathf.Sin(angle) * orbitRadius;

            Vector3 localOffset = new Vector3(x, 0f, z);

            projectile.transform.position = player.position + (player.rotation * localOffset);
        }
    }
}