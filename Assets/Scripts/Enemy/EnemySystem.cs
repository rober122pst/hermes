using UnityEngine;

public class EnemySystem : MonoBehaviour, IDamageable
{
    public EnemiesConfig config;

    DamageReceiver damageReceiver;

    private float life;
    private float speed = 0;
    private float targetSpd = 0;
    private AudioSource audioSource;

    [Tooltip("Tempo máximo (em segundos) entre os hits para os danos serem somados no mesmo popup")]
    public float combineTimeWindow = 0.1f;
    public Transform popupSpawnPoint;
    private DamagePopup lastPopup;
    private float lastDamageTime = -1f;

    Transform player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        damageReceiver = gameObject.GetComponent<DamageReceiver>();
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

        transform.LookAt(player);
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

        gameObject.SetActive(false);
        CinemachineShake.Instance.ShakeCamera(1f, 0.25f);
    }

    public void TakeDamage(float damage)
    {
        Debug.Log("Dano sofrido: " + damage);
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
