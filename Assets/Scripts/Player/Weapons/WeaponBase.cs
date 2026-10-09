using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    protected WeaponData weaponData;
    protected Transform playerTransform;
    protected Dictionary<Collider, float> hitCooldowns = new Dictionary<Collider, float>();
    protected List<Collider> collidersToRemove = new List<Collider>();

    public virtual void Initialize(WeaponData data, Transform player)
    {
        weaponData = data;
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
}
