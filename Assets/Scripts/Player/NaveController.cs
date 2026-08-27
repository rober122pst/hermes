using UnityEngine;
using System.Collections.Generic;
namespace Player
{
    public class NaveController : MonoBehaviour
    {
        [Header("Configurações de Movimento")]
        public float velocidade = 1f;
        public float velocidadeSprint = 5f;

        [Header("Configurações de Rotação (Espaço)")]
        [SerializeField]
        float multAnguloVelocidade = 0.5f;
        [SerializeField]
        float multVelocidadeRotacao = 0.05f;

        [Header("Configurações de Tiro")]
        [SerializeField]
        float tempoEntreTiros = 0.5f;
        float tempoUltimoTiro = 0f;

        private Rigidbody rb;
        private StarterAssetsInputs input;

        [Space(10)]
        [SerializeField]
        List<ObjectPool> bulletPools;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            input = GetComponent<StarterAssetsInputs>();
        }

        private void Update()
        {
            if (input.fire && Time.time > tempoUltimoTiro)
            {
                for (int i = 0; i < bulletPools.Count; i++)
                {
                    GameObject bullet = bulletPools[i].GetInstance();
                    bullet.transform.position = bulletPools[i].transform.position;
                    bullet.transform.rotation = bulletPools[i].transform.rotation;
                    bullet.SetActive(true);
                }
                tempoUltimoTiro = Time.time + tempoEntreTiros;
            }
        }

        private void FixedUpdate()
        {
            float velocidadeAlvo = input.sprint ? velocidadeSprint : velocidade;

            rb.AddForce(rb.transform.TransformDirection(Vector3.forward) * input.move.y * velocidadeAlvo, ForceMode.VelocityChange);
            rb.AddForce(rb.transform.TransformDirection(Vector3.right) * input.move.x * velocidadeAlvo, ForceMode.VelocityChange);

            rb.AddTorque(rb.transform.right * multAnguloVelocidade * input.look.y * -1, ForceMode.VelocityChange);
            rb.AddTorque(rb.transform.up * multAnguloVelocidade * input.look.x, ForceMode.VelocityChange);

            rb.AddTorque(rb.transform.forward * multVelocidadeRotacao * -input.roll.x, ForceMode.VelocityChange);
        }
    }
}