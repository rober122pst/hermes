using System.Collections.Generic;
using UnityEngine;

public class LaserWeapon : WeaponBase
{
    [Header("Configurações de Spawn")]
    public List<GameObject> spawnPoints;
    private List<(GameObject projectile, float lifetime)> activeProjectiles = new List<(GameObject, float)>();
    private float timer;

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        GameObject[] weaponsFind = GameObject.FindGameObjectsWithTag("LaserWeapon");
        spawnPoints = new List<GameObject>(weaponsFind);
        SpawnProjectiles();
    }

    private void SpawnProjectiles()
    {
        for (int i = 0; i < weaponData.projectileAmount; i++)
        {
            GameObject p = ObjectPool.Instance.GetInstance(weaponData.projectilePoolID);

            if (i < spawnPoints.Count)
            {
                p.transform.position = spawnPoints[i].transform.position;
                p.transform.rotation = spawnPoints[i].transform.rotation;
            }
            p.SetActive(true);
            activeProjectiles.RemoveAll(item => item.projectile == p);
            activeProjectiles.Add((p, weaponData.lifetime));
        }
    }

    void Update()
    {
        if (timer <= 0f)
        {
            SpawnProjectiles();
            timer = weaponData.projectileCooldown;
        }
        timer -= Time.deltaTime;
        UpdateMoves();
    }

    private void UpdateMoves()
    {
        for (int i = activeProjectiles.Count - 1; i >= 0; i--)
        {
            var activeProjectile = activeProjectiles[i];
            if (activeProjectile.projectile == null) continue;
            Debug.Log($"Projectile {activeProjectile.projectile.GetInstanceID()} updated on frame {Time.frameCount}");
            activeProjectile.projectile.transform.position += activeProjectile.projectile.transform.forward * weaponData.speed * Time.deltaTime;

            activeProjectile.lifetime -= Time.deltaTime;
            activeProjectiles[i] = activeProjectile;

            if (activeProjectiles[i].lifetime <= 0f)
            {
                activeProjectiles[i].projectile.SetActive(false);
                activeProjectiles.RemoveAt(i);
            }
        }
    }
}
