using UnityEngine;

public class AuraWeapon : WeaponBase
{
    public GameObject auraPrefab;
    private GameObject activeProjectile;

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        SpawnProjectiles();
    }

    private void SpawnProjectiles()
    {
        for (int i = 0; i < weaponData.projectileAmount; i++)
        {
            GameObject p = Instantiate(auraPrefab, playerTransform);
            // p.SetActive(true);
            activeProjectile = p;
        }
    }
}
