using UnityEngine;

namespace Enemy
{
    public class EnemySystem : MonoBehaviour
    {
        public EnemiesConfig config;

        DamageReceiver damageReceiver;

        private float life;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            damageReceiver = gameObject.GetComponent<DamageReceiver>();
            life = config.maxHealth;
        }

        // Update is called once per frame
        void Update()
        {
            if (life <= 0f)
            {
                Die();
            }
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

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Bullets"))
            {
                Bullet bullet = other.GetComponent<Bullet>();
                float dano = bullet.GetDamage();
                damageReceiver.TakeDamage(dano);
                life -= dano;
            }
        }
    }
}
