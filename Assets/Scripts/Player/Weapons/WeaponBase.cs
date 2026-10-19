using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    public WeaponData weaponData;
    protected Transform playerTransform;
    protected Dictionary<Collider, float> hitCooldowns = new Dictionary<Collider, float>();
    protected List<Collider> collidersToRemove = new List<Collider>();
    protected int level = 1;

    protected bool updated = false;

    public virtual void Initialize(WeaponData data, Transform player)
    {
        weaponData = Instantiate(data);
        playerTransform = player;
    }

    protected virtual bool CanHitTarget(Collider target)
    {
        if (!hitCooldowns.ContainsKey(target))
        {
            return true;
        }
        return Time.time >= hitCooldowns[target] + weaponData.projectileCooldown;
    }
    protected virtual void CleanUpCooldowns()
    {
        collidersToRemove.Clear();

        foreach (var kvp in hitCooldowns)
        {
            if (kvp.Key == null || !kvp.Key.gameObject.activeInHierarchy || Time.time >= kvp.Value + weaponData.projectileCooldown)
            {
                collidersToRemove.Add(kvp.Key);
            }
        }

        for (int i = 0; i < collidersToRemove.Count; i++)
        {
            hitCooldowns.Remove(collidersToRemove[i]);
        }
    }

    public void UpdateWeapon(WeaponStatsModifier modifier)
    {
        weaponData.attackCooldown *= modifier.attackCooldown;
        weaponData.projectileCooldown *= modifier.projectileCooldown;
        weaponData.projectileAmount += modifier.projectileAmount;
        weaponData.projectileScale *= modifier.projectileScale;
        weaponData.speed *= modifier.speed;
        weaponData.lifetime *= modifier.lifetime;
        weaponData.damage *= modifier.damage;
        level++;
        updated = true;
    }
}
