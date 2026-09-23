using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    public void EquipWeapon(WeaponData newWeapon)
    {
        if (newWeapon.weaponLogicPrefab != null)
        {
            GameObject weaponInstance = Instantiate(newWeapon.weaponLogicPrefab, transform);

            WeaponBase logic = weaponInstance.GetComponent<WeaponBase>();
            if (logic != null)
            {
                logic.Initialize(newWeapon, transform);
            }
        }
    }
}
