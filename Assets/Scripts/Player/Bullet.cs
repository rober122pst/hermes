using System.Collections;
using UnityEngine;

public interface IDoDamage
{
    void DoDamage(float damage);
}

public interface ITargetingStrategy
{
    // Retorna a direção ou o alvo para onde o ataque deve ir
    Vector3 GetTargetDirection(Transform playerTransform);
}

public interface IMovementStrategy
{
    // Move o projétil baseado na lógica (ex: Parábola, Bumerangue, Orbital)
    void Move(Transform projectile, Vector3 direction, float speed);
}

public class Bullet : MonoBehaviour
{
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private float speed = 50f;
    [SerializeField] private float damage;
    public IDoDamage damageType;

    [Header("Configuração da arma")]
    [SerializeField] private WeaponConfigSO weaponConfig;
    [SerializeField] private ProjectileManager projectileManager;

    private float cooldownTimer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        StartCoroutine(DisableRoutine());
        cooldownTimer = weaponConfig.cooldown;
    }

    IEnumerator DisableRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        gameObject.SetActive(false);
    }

    IEnumerator CanFire()
    {
        yield return new WaitForSeconds(cooldownTimer);
        TryDoAttack();
    }

    public void TryDoAttack()
    {
        Vector3 fireDirection = weaponConfig.Targeting?.GetTargetDirection(transform);
        weaponConfig.DoDamage?.DoDamage(damage);
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
        IDamageable damageable = coll.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            gameObject.SetActive(false);
        }
    }
}
