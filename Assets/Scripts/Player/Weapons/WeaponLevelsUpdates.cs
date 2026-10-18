using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public struct WeaponStatsModifier
{
    public float attackCooldown;
    public float projectileCooldown;
    public int projectileAmount;
    public float projectileScale;
    public float speed;
    public float lifetime;
    public float damage;
    public string description;
}


[CreateAssetMenu(fileName = "WeaponLevelsUpdates", menuName = "Weapon/WeaponLevelsUpdates")]
public class WeaponLevelsUpdates : ScriptableObject
{
    public List<WeaponStatsModifier> weaponLevels = Enumerable.Range(0, 7)
    .Select(_ => new WeaponStatsModifier
    {
        attackCooldown = 1,
        projectileCooldown = 1,
        projectileAmount = 0,
        projectileScale = 1,
        speed = 1,
        lifetime = 1,
        damage = 1,
        description = ""
    }).ToList();
}
