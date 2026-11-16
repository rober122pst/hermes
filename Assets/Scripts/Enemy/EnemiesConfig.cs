using UnityEngine;

public enum EnemyType
{
    Basic,
    Fast,
    Tank,
    Boss
}

[CreateAssetMenu(fileName = "EnemiesConfig", menuName = "Game/Enemy/EnemiesConfig")]
public class EnemiesConfig : ScriptableObject
{
    [Header("Configuração do inimigo")]
    public EnemyType enemyType;
    public GameObject enemyPrefab;

    [Header("Atributos do inimigo")]
    public float maxHealth;
    public float speed;
    public PoolID bulletPool;
    public float damage;
    public float attackRange;
    public float attackCooldown;
    public int cost;

    public float distanceToStop = 1.5f;
    [Min(0f)] public float windupTime = 0.35f;
    [Min(0f)] public float projectileSpeed = 40f;
    [Min(0f)] public float projectileTurnSpeed;

    [Header("Áudio")]
    public AudioClip hitAudio;
}
