using UnityEngine;
using System.Collections.Generic;

namespace Player
{
    [System.Serializable]
    public struct Stats
    {
        [Header("Dados do player")]
        [SerializeField] private float speed;
        [SerializeField] private float sprintSpeed;
        [SerializeField] private float experience;
        [SerializeField] private float maxExperience;
        [SerializeField] private float incrementExperience;
        [SerializeField] private float magnetismForce;

        public float Speed => speed;
        public float SprintSpeed => sprintSpeed;
        public float Experience => experience;
        public float MaxExperience => maxExperience;
        public float IncrementExperience => incrementExperience;
        public float MagnetismForce => magnetismForce;

        public void AddExperience(float amount)
        {
            experience += amount;
            if (experience >= maxExperience)
            {
                experience -= maxExperience;
                maxExperience *= 1.5f;
                UIManager.Instance.ShowLevelUpScreen();
            }
        }
    }

    public class NaveController : MonoBehaviour
    {
        public Stats stats = new Stats();

        [Header("Configurações de Rotação")]
        [SerializeField]
        float multAnguloVelocidade = 0.5f;
        [SerializeField]
        float multVelocidadeRotacao = 0.05f;


        [Header("Audios")]
        [SerializeField]
        private AudioClip tiroAudioClip;

        private Rigidbody rb;
        private StarterAssetsInputs input;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            input = GetComponent<StarterAssetsInputs>();
        }

        private void FixedUpdate()
        {
            float velocidadeAlvo = input.sprint ? stats.SprintSpeed : stats.Speed;

            rb.AddForce(rb.transform.TransformDirection(Vector3.forward) * input.move.y * velocidadeAlvo, ForceMode.VelocityChange);
            rb.AddForce(rb.transform.TransformDirection(Vector3.right) * input.move.x * velocidadeAlvo, ForceMode.VelocityChange);

            rb.AddTorque(rb.transform.right * multAnguloVelocidade * input.look.y * -1, ForceMode.VelocityChange);
            rb.AddTorque(rb.transform.up * multAnguloVelocidade * input.look.x, ForceMode.VelocityChange);

            rb.AddTorque(rb.transform.forward * multVelocidadeRotacao * -input.roll.x, ForceMode.VelocityChange);
        }


    }
}