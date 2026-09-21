using UnityEngine;


public class WeaponManager : MonoBehaviour
{
    public WeaponBase weapon;
    public float damage;
    public PoolID poolID;

    void Update()
    {
        weapon.WeaponBehaviour(poolID);
    }
}
