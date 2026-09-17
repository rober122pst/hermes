using UnityEngine;

[CreateAssetMenu(fileName = "WeaponConfigSO", menuName = "Weapon/Weapon Config")]
public class WeaponConfigSO : ScriptableObject
{
    [Header("Stats Base")]
    public string weaponName;
    public Sprite weaponSprite;
    public float cooldown = 1f;
    public int baseDamage = 10;
    public float projectileSpeed = 20f;
    public int piercing = 0;
    public float projectileLifetime = 2f;

    [Header("Estratégias")]
    public ScriptableObject targetingStrategy;
    public ScriptableObject movementStrategy;
    public ScriptableObject doDamage;

    public ITargetingStrategy Targeting => targetingStrategy as ITargetingStrategy;
    public IMovementStrategy Movement => movementStrategy as IMovementStrategy;
    public IDoDamage DoDamage => doDamage as IDoDamage;
}
