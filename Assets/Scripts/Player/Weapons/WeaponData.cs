using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Weapon/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Configurações da Arma")]
    public float attackCooldown = 2f;
    public float projectileCooldown = 0.2f;
    public int projectileAmount = 2;
    [Range(1f, 2f)]
    public float projectileScale = 1f;
    [Range(1f, 2f)]
    public float speedMult = 1f;
    public float speed = 5f;
    public float lifetime = 2f;

    [Header("Dependencias")]
    public GameObject weaponLogicPrefab;
    public PoolID projectilePoolID;
}