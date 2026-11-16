using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponManager : MonoBehaviour
{
    private const int MaxLevel = 8;

    [Header("Todas as armas do jogo")]
    public List<WeaponData> allWeapons;
    public List<RawImage> weaponsSlots;
    [SerializeField] private WeaponData firstWeapon;

    private readonly List<(WeaponData data, int level, WeaponBase logic)> equippedWeapons = new();

    public static WeaponManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        EquipWeapon(firstWeapon);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void EquipWeapon(WeaponData newWeapon)
    {
        if (!CanEquip(newWeapon)) return;

        int index = FindEquipped(newWeapon.weaponName);
        if (index >= 0)
        {
            var item = equippedWeapons[index];
            item.logic.UpdateWeapon(item.data.updates.weaponLevels[item.level - 1]);
            equippedWeapons[index] = (item.data, item.level + 1, item.logic);
            return;
        }

        WeaponBase logic = Instantiate(newWeapon.weaponLogicPrefab.GetComponent<WeaponBase>(), transform);
        RawImage slot = weaponsSlots[equippedWeapons.Count];
        equippedWeapons.Add((newWeapon, 1, logic));
        if (slot != null)
        {
            slot.enabled = true;
            slot.texture = newWeapon.weaponSprite;
        }
        logic.Initialize(newWeapon, transform);
    }

    public bool HasUpgradeableWeapons()
    {
        return allWeapons != null && allWeapons.Exists(CanEquip);
    }

    public List<WeaponData> GetRandomWeapons()
    {
        List<WeaponData> candidates = allWeapons.FindAll(CanEquip);
        List<WeaponData> offers = new();
        while (offers.Count < 3 && candidates.Count > 0)
        {
            int index = Random.Range(0, candidates.Count);
            offers.Add(candidates[index]);
            candidates.RemoveAt(index);
        }
        return offers;
    }

    private bool CanEquip(WeaponData weapon)
    {
        if (weapon == null) return false;

        int index = FindEquipped(weapon.weaponName);
        if (index < 0)
        {
            return weaponsSlots != null && equippedWeapons.Count < weaponsSlots.Count &&
                weapon.weaponLogicPrefab != null && weapon.weaponLogicPrefab.GetComponent<WeaponBase>() != null;
        }

        var item = equippedWeapons[index];
        return item.level < MaxLevel && item.data.updates != null &&
            item.level <= item.data.updates.weaponLevels.Count;
    }

    private int FindEquipped(string weaponName)
    {
        return equippedWeapons.FindIndex(item => item.data.weaponName == weaponName);
    }

    public int GetLevelByName(string weaponName)
    {
        int index = FindEquipped(weaponName);
        return index >= 0 ? equippedWeapons[index].level : 0;
    }
}
