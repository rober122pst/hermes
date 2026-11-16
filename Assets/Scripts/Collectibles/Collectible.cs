using Player;
using UnityEngine;

public abstract class Collectible : MonoBehaviour
{
    [SerializeField] protected NaveController player;
    [SerializeField] private float speed = 32f;

    private bool attracting;

    private void OnEnable()
    {
        attracting = false;
        if (player == null) player = FindFirstObjectByType<NaveController>();
    }

    private void Update()
    {
        if (player == null || !player.IsAlive || Time.deltaTime <= 0f) return;

        Vector3 toPlayer = player.transform.position - transform.position;
        float range = player.stats.MagnetismForce;
        attracting |= toPlayer.sqrMagnitude <= range * range;
        if (!attracting) return;

        const float collectRadius = 1.1f;
        float step = Mathf.Max(speed, player.stats.SprintSpeed + 5f) * Time.deltaTime;
        if (toPlayer.sqrMagnitude <= (step + collectRadius) * (step + collectRadius))
        {
            Collected();
            return;
        }

        transform.position += toPlayer.normalized * step;
    }

    public virtual void Collected()
    {
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isActiveAndEnabled && player != null && player.IsAlive &&
            other.GetComponentInParent<NaveController>() == player)
        {
            Collected();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (player == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, player.stats.MagnetismForce);
    }
}
