using System;
using Player;
using UnityEngine;

namespace Game.Player
{
    [System.Serializable]
    public struct Stats
    {
        [Header("Player Data")]
        [SerializeField] private float speed;
        [SerializeField] private float sprintSpeed;
        [SerializeField] private float experience;
        [SerializeField] private float maxExperience;
        [SerializeField] private float incrementExperience;
        [SerializeField] private float magnetismForce;

        public event Action<float> OnExperienceChanged;
        public event Action OnLevelUp;

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

                    if (experience >= maxExperience)
                    {
                        OnLevelUp?.Invoke();
                    }
                }
            }
        }

        public float MaxExperience
        {
            get => maxExperience;
            set => maxExperience = value;
        }

        public float IncrementExperience => incrementExperience;
        public float MagnetismForce => magnetismForce;
    }

    public class ShipController : MonoBehaviour
    {
        public Stats stats = new Stats();

        [Header("Rotation Settings")]
        [SerializeField] private float angleSpeedMultiplier = 0.5f;
        [SerializeField] private float rotationSpeedMultiplier = 0.05f;

        [Header("Audio")]
        [SerializeField] private AudioClip shootAudioClip;

        private Rigidbody rb;
        private StarterAssetsInputs input;

        [Header("Interaction")]
        [SerializeField] private float detectionRadius;
        [SerializeField] private LayerMask searchLayer;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            input = GetComponent<StarterAssetsInputs>();

            stats.OnLevelUp += HandleLevelUp;
        }

        private void OnDestroy()
        {
            stats.OnLevelUp -= HandleLevelUp;
        }

        private void HandleLevelUp()
        {
            if (global::UIManager.Instance != null && !global::UIManager.Instance.LevelUpScreen.activeInHierarchy)
            {
                stats.Experience -= stats.MaxExperience;
                stats.MaxExperience *= 1.1f;
                global::UIManager.Instance.LevelUpScreen.SetActive(true);
            }
        }

        private void FixedUpdate()
        {
            if (input == null) return;

            float targetSpeed = input.sprint ? stats.SprintSpeed : stats.Speed;

            rb.AddForce(rb.transform.TransformDirection(Vector3.forward) * input.move.y * targetSpeed, ForceMode.VelocityChange);
            rb.AddForce(rb.transform.TransformDirection(Vector3.right) * input.move.x * targetSpeed, ForceMode.VelocityChange);

            rb.AddTorque(rb.transform.right * angleSpeedMultiplier * input.look.y * -1, ForceMode.VelocityChange);
            rb.AddTorque(rb.transform.up * angleSpeedMultiplier * input.look.x, ForceMode.VelocityChange);

            rb.AddTorque(rb.transform.forward * rotationSpeedMultiplier * -input.roll.x, ForceMode.VelocityChange);
        }
    }
}
