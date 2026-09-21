using UnityEngine;

public class Projectile : MonoBehaviour
{
    public WeaponBase weaponConfig;
    public Transform player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        weaponConfig.Attack(10, gameObject, player);
    }

    public void SetWeaponConfig(WeaponBase weapon)
    {
        weaponConfig = weapon;
    }
}
