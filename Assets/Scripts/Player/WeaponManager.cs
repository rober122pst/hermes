using UnityEngine;


public class WeaponManager : MonoBehaviour
{
    public WeaponBase weapon;
    public float damage;
    public GameObject projectile;

    void Start()
    {
        weapon.WeaponBehaviour(projectile);
    }
}
