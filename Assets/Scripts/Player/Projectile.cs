using UnityEngine;

public class Projectile : MonoBehaviour
{
    public WeaponBase weaponConfig;
    public Transform player;
    float attackTimer = 0;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (attackTimer <= 0)
        {
            weaponConfig.Attack(10, gameObject, player);
        }

        attackTimer -= Time.deltaTime;
    }

    public void SetWeaponConfig(WeaponBase weapon)
    {
        weaponConfig = weapon;
    }
}
