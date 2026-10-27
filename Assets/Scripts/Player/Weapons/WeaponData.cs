using System;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Weapon/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Configurações da Arma")]
    public string weaponName;
    public Texture weaponSprite;
    public float attackCooldown = 2f;
    public float projectileCooldown = 0.2f;
    public int projectileAmount = 2;
    [Range(1f, 2f)]
    [SerializeField] private float _projectileScale = 1f;
    public float speed = 5f;
    public float lifetime = 2f;
    public float damage = 5f;

    [Header("Dependencias")]
    public GameObject weaponLogicPrefab;
    public PoolID projectilePoolID;
    public WeaponLevelsUpdates updates;

    public Action OnScale;
    public float projectileScale
    {
        get => _projectileScale;
        set
        {
            if (value != _projectileScale)
            {
                _projectileScale = value;
                OnScale?.Invoke();
            }
        }
    }
}