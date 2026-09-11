using System.Collections;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private float speed = 50f;
    [SerializeField]
    float damage;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        StartCoroutine(DisableRoutine());
    }

    IEnumerator DisableRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.forward * Time.deltaTime * speed);
    }

    public void SetDamage(float _damage)
    {
        damage = _damage;
    }

    void OnTriggerEnter(Collider coll)
    {
        EnemySystem damageable = coll.GetComponentInParent<EnemySystem>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            gameObject.SetActive(false);
        }
    }
}
