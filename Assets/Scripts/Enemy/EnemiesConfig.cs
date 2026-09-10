using UnityEngine;

public enum EnemyType
{
    Basic,
    Fast,
    Tank
}

[CreateAssetMenu(fileName = "EnemiesConfig", menuName = "Game/Enemy/EnemiesConfig")]
public class EnemiesConfig : ScriptableObject
{
    [Header("Enemy Configuration")]
    public EnemyType enemyType;
    public GameObject enemyPrefab;

    [Header("Enemy Stats")]
    public float maxHealth;
    public float speed;
    public float damage;
    public float attackRange;
    public float attackCooldown;
    public int cost;

    public float distanceToStop = 1.5f;
}
