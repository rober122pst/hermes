using System.Collections.Generic;
using UnityEngine;

public class OrbitalWeapon : WeaponBase
{
    [Header("Configurações Orbitais")]
    public float orbitRadius = 10f;
    private float currentAngle = 0f;
    private List<GameObject> activeProjectiles = new List<GameObject>();

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        SpawnProjectiles();
    }

    private void SpawnProjectiles()
    {
        for (int i = 0; i < weaponData.projectileAmount; i++)
        {
            GameObject p = ObjectPool.Instance.GetInstance(weaponData.projectilePoolID);
            p.SetActive(true);
            activeProjectiles.Add(p);
        }
    }

    void Update()
    {
        currentAngle += weaponData.speed * Time.deltaTime;
        UpdateOrbits();
    }

    private void UpdateOrbits()
    {
        for (int i = 0; i < activeProjectiles.Count; i++)
        {
            if (activeProjectiles[i] == null) continue;

            float angle = currentAngle + 360f / weaponData.projectileAmount * i;
            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * orbitRadius;
            float z = Mathf.Sin(angle * Mathf.Deg2Rad) * orbitRadius;

            Vector3 localOffset = new Vector3(x, 0f, z);
            activeProjectiles[i].transform.position = playerTransform.position + (playerTransform.rotation * localOffset);
        }
    }
}
