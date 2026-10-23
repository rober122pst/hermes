using UnityEngine;
using System.Collections.Generic;
using System;

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

        public event Action<float> OnExperienceChanged;

        public float Speed => speed;
        public float SprintSpeed => sprintSpeed;
        public float Experience
        {
            get => experience;
            set
            {
                if (experience != value)
                {
                    experience = value;
                    OnExperienceChanged?.Invoke(experience);
                }
            }
        }
        public float MaxExperience
        {
            get => maxExperience;
            set
            {
                if (maxExperience != value)
                {
                    maxExperience = value;
                }
            }
        }
        public float IncrementExperience => incrementExperience;
        public float MagnetismForce => magnetismForce;
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

        private void Update()
        {
            if (stats.Experience >= stats.MaxExperience)
            {
                if (!UIManager.Instance.LevelUpScreen.activeInHierarchy)
                {
                    stats.Experience -= stats.MaxExperience;
                    stats.MaxExperience *= 1.1f;
                    UIManager.Instance.LevelUpScreen.SetActive(true);
                }
            }
        }
    }
}