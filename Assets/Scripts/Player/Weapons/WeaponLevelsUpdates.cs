using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponLevelsUpdates", menuName = "Weapon/WeaponLevelsUpdates")]
public class WeaponLevelsUpdates : ScriptableObject
{
    public List<WeaponStatsModifier> weaponLevels = Enumerable.Range(0, 8)
    .Select(_ => new WeaponStatsModifier
    {
        attackCooldown = 1,
        projectileCooldown = 1,
        projectileAmount = 0,
        projectileScale = 1,
        speed = 1,
        lifetime = 1,
        damage = 1
    }).ToList();
}
