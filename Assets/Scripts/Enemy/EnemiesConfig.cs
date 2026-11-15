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

    [Header("Áudio")]
    public AudioClip hitAudio;
}
