using UnityEngine;

namespace Enemy
{
    public class EnemySystem : MonoBehaviour, IDamageable
    {
        public EnemiesConfig config;

        DamageReceiver damageReceiver;

        private float life;
        private float speed = 0;
        private float targetSpd = 0;
        Transform player;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            damageReceiver = gameObject.GetComponent<DamageReceiver>();
            life = config.maxHealth;
            player = GameObject.FindGameObjectWithTag("Player").transform;
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
            damageReceiver.TakeDamage(damage);
            life -= damage;

            if (life <= 0)
            {
                Die();
            }
        }


        //private void OnTriggerEnter(Collider other)
        //{
        //    if (other.CompareTag("Bullets"))
        //    {
        //        Bullet bullet = other.GetComponent<Bullet>();
        //        float dano = bullet.GetDamage();
        //        damageReceiver.TakeDamage(dano);
        //        life -= dano;
        //    }
        //}
    }
}
