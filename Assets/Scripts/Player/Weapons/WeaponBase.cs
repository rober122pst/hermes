using System;
using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    protected WeaponData weaponData;
    protected Transform playerTransform;

    public virtual void Initialize(WeaponData data, Transform player)
    {
        weaponData = data;
        playerTransform = player;
    }
}
