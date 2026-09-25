using UnityEngine;

public class Projectile : MonoBehaviour
{
    void OTriggerEnter(Collider other)
    {
        IDamageable damageable = other.gameObject.GetComponentInChildren<IDamageable>();
        if (damageable != null)
        {

        }
    }
}
