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
            equipedWeapons[index] = (level + 1, equipedWeapons[index].weapon);

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

    public List<WeaponData> GetRandomWeapons()
    {
        List<WeaponData> candidatePool = new List<WeaponData>();
        List<WeaponData> drawnWeapons = new List<WeaponData>();

        int maxEquipped = weaponsSlots.Count;
        int maxLevel = 8;

        // Se atingiu o limite de armas equipadas, filtra apenas as que o jogador já possui no inventário
        if (equipedWeapons.Count >= maxEquipped)
        {
            foreach (var item in equipedWeapons)
            {
                if (item.level < maxLevel)
                {
                    candidatePool.Add(item.weapon.weaponData);
                }
            }
        }
        // Se ainda há espaço sobrando, libera todas as armas (exceto as equipadas que já estão no nível 8)
        else
        {
            foreach (WeaponData weapon in allWeapons)
            {
                int equippedIndex = equipedWeapons.FindIndex(
                    item => item.weapon.weaponData.weaponName == weapon.weaponName
                );

                // Entra no sorteio se não estiver equipada OU se estiver equipada abaixo do nível 8
                if (equippedIndex == -1 || equipedWeapons[equippedIndex].level < maxLevel)
                {
                    candidatePool.Add(weapon);
                }
            }
        }

        // Sorteia até 3 armas sem repetir a mesma arma no mesmo sorteio
        int drawCount = Mathf.Min(3, candidatePool.Count);
        for (int i = 0; i < drawCount; i++)
        {
            int randomIndex = Random.Range(0, candidatePool.Count);
            drawnWeapons.Add(candidatePool[randomIndex]);
            candidatePool.RemoveAt(randomIndex); // Remove da piscina temporária para evitar duplicatas
        }

        return drawnWeapons;
    }
}
