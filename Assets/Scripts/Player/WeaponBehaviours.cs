using UnityEngine;

[CreateAssetMenu(fileName = "WeaponBehaviours", menuName = "Weapon/Base")]
public class WeaponBase : ScriptableObject
{
    public virtual void Attack(float damage, GameObject projectile, Transform player)
    {
        Debug.Log("Movendo.");
    }

    public virtual void WeaponBehaviour(GameObject projectile) { }
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
    [Header("Configurações da Arma")]
    public float attackCooldown = 2f;
    public float projectileCooldown = 0.2f;
    public int projectileAmount = 2;

    [Header("Projectile Config")]
    public float orbitRadius = 10f;
    public float speed = 5f;
    float currentAngle = 0f;

    public override void WeaponBehaviour(GameObject projectile)
    {
        GameObject proj = Instantiate(projectile);
        Projectile p = proj.AddComponent<Projectile>();
        p.SetWeaponConfig(this);
    }

    public override void Attack(float damage, GameObject projectile, Transform player)
    {
        if (projectile == null || player == null) return;

        currentAngle += speed * Time.deltaTime;

        float x = Mathf.Cos(currentAngle) * orbitRadius;
        float z = Mathf.Sin(currentAngle) * orbitRadius;

        Vector3 localOffset = new Vector3(x, 0f, z);

        projectile.transform.position = player.position + (player.rotation * localOffset);
    }
}