using UnityEngine;

namespace Enemy.System
{
    public class EnemySystem : MonoBehaviour
    {
        [Header("Dados do Inimigo")]
        public float Vida = 100f;

        DamageReceiver damageReceiver;
        [SerializeField]
        CinemachineShake cinemachineShake;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            damageReceiver = gameObject.GetComponent<DamageReceiver>();
        }

        // Update is called once per frame
        void Update()
        {
            if (Vida <= 0f)
            {
                gameObject.SetActive(false);
                cinemachineShake.ShakeCamera(1f, 0.25f);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Bullets"))
            {
                Bullet bullet = other.GetComponent<Bullet>();
                float dano = bullet.GetDamage();
                damageReceiver.TakeDamage(dano);
                Vida -= dano;
            }
        }
    }
}
