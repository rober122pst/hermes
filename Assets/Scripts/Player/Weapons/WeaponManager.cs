using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponManager : MonoBehaviour
{
    [Header("Todas as armas do jogo")]
    public List<WeaponData> allWeapons;
    public static WeaponManager Instance { get; private set; }
    public List<RawImage> weaponsSlots;
    private List<(int level, WeaponBase weapon)> equipedWeapons = new List<(int, WeaponBase)>();
    [SerializeField]
    private WeaponData firstWeapon;

    private void Start()
    {
        Instance = this;
        EquipWeapon(firstWeapon);
    }

    public void EquipWeapon(WeaponData newWeapon)
    {
        int index = equipedWeapons.FindIndex(item => item.weapon.weaponData.weaponName == newWeapon.weaponName);

        if (index != -1)
        {
            int level = equipedWeapons[index].level;
            WeaponBase weapon = equipedWeapons[index].weapon;
            List<WeaponStatsModifier> weaponLevels = weapon.weaponData.updates.weaponLevels;

            if (level == 8) return;

            UpdateWeapon(weaponLevels[level - 1], weapon.weaponData);
            // equipedWeapons[index] = (level++, equipedWeapons[index].weapon);

            return;
        }

        if (newWeapon.weaponLogicPrefab != null)
        {
            GameObject weaponInstance = Instantiate(newWeapon.weaponLogicPrefab, transform);

            WeaponBase logic = weaponInstance.GetComponent<WeaponBase>();
            if (logic != null)
            {
                WeaponBase weaponBase = weaponInstance.GetComponent<WeaponBase>();
                equipedWeapons.Add((1, weaponBase));
                weaponsSlots[equipedWeapons.Count - 1].enabled = true;
                weaponsSlots[equipedWeapons.Count - 1].texture = newWeapon.weaponSprite;
                logic.Initialize(newWeapon, transform);
            }
        }
    }

    public void UpdateWeapon(WeaponStatsModifier modifier, WeaponData weaponData)
    {
        weaponData.attackCooldown *= modifier.attackCooldown;
        weaponData.projectileCooldown *= modifier.projectileCooldown;
        weaponData.projectileAmount += modifier.projectileAmount;
        weaponData.projectileScale *= modifier.projectileScale;
        weaponData.speed *= modifier.speed;
        weaponData.lifetime *= modifier.lifetime;
        weaponData.damage *= modifier.damage;
    }
}
