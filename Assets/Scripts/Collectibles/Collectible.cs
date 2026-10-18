using Player;
using UnityEngine;

public abstract class Collectible : MonoBehaviour
{
    [SerializeField]
    protected NaveController player;

    [Header("Distancia e Movimento")]
    [SerializeField] private float speed = 5f;

    private float currentSpeed;

    private void Start()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<NaveController>();
        }
    }

    private void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        float targetSpeed = distanceToPlayer <= player.stats.MagnetismForce ? speed : 0;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, 0.3f);

        transform.position = Vector3.MoveTowards(
            transform.position,
            player.transform.position,
            targetSpeed * Time.deltaTime
        );

        Vector3 direction = new Vector3(player.transform.position.x, transform.position.x, player.transform.position.z);
        transform.LookAt(direction);
    }

    private void OnDrawGizmosSelected()
    {
        // Raio de detecção (Amarelo)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, player.stats.MagnetismForce);
    }

    public virtual void Collected()
    {
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            Collected();
        }
    }
}
