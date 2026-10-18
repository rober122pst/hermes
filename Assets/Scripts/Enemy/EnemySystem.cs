using UnityEngine;

public class EnemySystem : MonoBehaviour, IDamageable
{
    public EnemiesConfig config;

    private float life;
    private float speed = 0;
    private float targetSpd = 0;
    private AudioSource audioSource;

    [Tooltip("Tempo máximo (em segundos) entre os hits para os danos serem somados no mesmo popup")]
    public float combineTimeWindow = 0.1f;
    public Transform popupSpawnPoint;
    private DamagePopup lastPopup;
    private float lastDamageTime = -1f;

    private float attackTimer = 0f;

    Transform player;

    public PoolID gemPool;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        life = config.maxHealth;
        player = GameObject.FindGameObjectWithTag("Player").transform;
        audioSource = player.GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        Walk();
    }

    void Walk()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        targetSpd = distance > config.distanceToStop ? config.speed : 0f;

        speed = Mathf.Lerp(speed, targetSpd, Time.deltaTime * 2f);
        Vector3 direcao = (player.position - transform.position).normalized;
        transform.position += direcao * speed * Time.deltaTime;

        if (speed <= 0.2f)
        {
            Attack();
        }

        transform.LookAt(player);
    }

    void Attack()
    {
        if (attackTimer <= 0f)
        {
            GameObject p = ObjectPool.Instance.GetInstance(config.bulletPool);
            p.SetActive(true);
            p.transform.position = transform.position;
            p.transform.rotation = transform.rotation;
            Projectile.Instance.AddBullet(p);
            attackTimer = config.attackCooldown;
        }
        attackTimer -= Time.deltaTime;
    }

    void Die()
    {
        if (WaveManager.Instance != null)
        {

        }
        else
        {
            Debug.LogWarning("WaveManager instance is null. Cannot register defeated enemy.");
        }

        GameObject gem = ObjectPool.Instance.GetInstance(gemPool);
        gem.transform.position = gameObject.transform.position;
        gem.SetActive(true);
        CinemachineShake.Instance.ShakeCamera(1f, 0.25f);

        gameObject.SetActive(false);
    }

    public void TakeDamage(float damage)
    {
        audioSource.PlayOneShot(config.hitAudio);
        life -= damage;
        ShowDamagePopup(damage);

        if (life <= 0)
        {
            Die();
        }
    }

    void ShowDamagePopup(float damageAmount)
    {
        // Se existe um popup ativo e o tempo atual está dentro da janela permitida
        if (lastPopup != null && Time.time <= lastDamageTime + combineTimeWindow)
        {
            lastPopup.AddDamage(damageAmount);
        }
        else
        {
            // O tempo expirou ou é o primeiro dano: cria um novo popup
            Vector3 spawnPos = popupSpawnPoint != null ? popupSpawnPoint.position : transform.position;

            GameObject popupObj = DamageManager.Instance.ShowDamage(damageAmount, spawnPos);

            lastPopup = popupObj.GetComponent<DamagePopup>();
        }

        // Atualiza o tempo do último dano recebido
        lastDamageTime = Time.time;
    }
}
