using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponManager : MonoBehaviour
{
    public List<RawImage> weaponsSlots;
    private List<GameObject> equipedWeapons = new List<GameObject>();

    public void EquipWeapon(WeaponData newWeapon)
    {
        if (newWeapon.weaponLogicPrefab != null)
        {
            GameObject weaponInstance = Instantiate(newWeapon.weaponLogicPrefab, transform);

            WeaponBase logic = weaponInstance.GetComponent<WeaponBase>();
            if (logic != null)
            {
                equipedWeapons.Add(weaponInstance);
                weaponsSlots[equipedWeapons.Count - 1].enabled = true;
                weaponsSlots[equipedWeapons.Count - 1].texture = newWeapon.weaponSprite;
                logic.Initialize(newWeapon, transform);
            }
        }
    }
}
