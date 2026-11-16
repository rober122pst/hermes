using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Player
{
    [Serializable]
    public struct Stats
    {
        [SerializeField] private float speed;
        [SerializeField] private float sprintSpeed;
        [SerializeField] private float experience;
        [SerializeField] private float maxExperience;
        [SerializeField] private float incrementExperience;
        [SerializeField] private float magnetismForce;

        public event Action<float> OnExperienceChanged;

        public float Speed => speed > 0f ? speed : 11f;
        public float SprintSpeed => sprintSpeed > 0f ? sprintSpeed : 19f;
        public float IncrementExperience => incrementExperience > 0f ? incrementExperience : 10f;
        public float MagnetismForce => magnetismForce > 0f ? magnetismForce : 24f;

        public float Experience
        {
            get => experience;
            set
            {
                if (experience == value) return;
                experience = value;
                OnExperienceChanged?.Invoke(experience);
            }
        }

        public float MaxExperience
        {
            get => maxExperience > 0f ? maxExperience : 50f;
            set
            {
                if (maxExperience == value) return;
                maxExperience = value;
                OnExperienceChanged?.Invoke(experience);
            }
        }
    }

    [RequireComponent(typeof(Rigidbody), typeof(StarterAssetsInputs))]
    public class NaveController : MonoBehaviour, IDamageable
    {
        public Stats stats = new Stats();

        [Header("Pilotagem")]
        [SerializeField, FormerlySerializedAs("multAnguloVelocidade")]
        [Tooltip("Graus de rotação por pixel de movimento do mouse.")]
        private float angleSpeedMultiplier = 0.14f;
        [SerializeField, FormerlySerializedAs("multVelocidadeRotacao")]
        private float rotationSpeedMultiplier = 0.15f;
        [SerializeField, Min(0.02f)] private float thrustResponseTime = 0.11f;
        [SerializeField, Min(0.02f)] private float coastResponseTime = 0.15f;
        [SerializeField, Min(0.01f)] private float turnResponseTime = 0.03f;
        [SerializeField] private float stickTurnRate = 145f;

        [Header("Cruzeiro orbital")]
        [SerializeField, Min(1f)] private float cruiseSpeed = 96f;
        [SerializeField, Min(0.02f)] private float cruiseResponseTime = 0.40f;
        [SerializeField, Min(0.02f)] private float cruiseBrakeTime = 0.27f;

        [Header("Integridade da nave")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float hitImmunity = 0.45f;

        private Rigidbody rb;
        private StarterAssetsInputs input;
        private InputAction lookAction;
        private Quaternion viewRotation;
        private Vector3 angularInputRate;
        private Vector3 thrustRate;
        private CollisionDetectionMode normalCollisionMode;
        private bool cruiseAvailable;
        private bool cruiseFullSpeed;
        private float nextDamageTime;

        public event Action<float, Vector3?> Damaged;

        public float Health { get; private set; }
        public float MaxHealth => maxHealth;
        public bool IsAlive => Health > 0f;
        public int GemsCollected { get; private set; }
        public bool IsCruising { get; private set; }
        public float MaxCruiseSpeed => cruiseSpeed;
        public float TravelSpeed => rb != null ? rb.linearVelocity.magnitude : 0f;
        public Quaternion ViewRotation => Application.isPlaying && rb != null ? viewRotation : transform.rotation;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            input = GetComponent<StarterAssetsInputs>();
            Health = maxHealth;
            viewRotation = rb.rotation;
            normalCollisionMode = rb.collisionDetectionMode;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            if (rb.interpolation == RigidbodyInterpolation.None) rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void Start()
        {
            if (TryGetComponent(out PlayerInput playerInput))
            {
                lookAction = playerInput.actions?.FindAction("Look");
            }
        }

        private void FixedUpdate()
        {
            if (!IsAlive) return;

            float dt = Time.fixedDeltaTime;
            rb.angularVelocity *= Mathf.Exp(-16f * dt);
            if (Mathf.Abs(Quaternion.Dot(rb.rotation, viewRotation)) < 0.9999999f)
                rb.MoveRotation(viewRotation);

            Vector2 move = Vector2.ClampMagnitude(input.move, 1f);
            bool cruiseThrust = cruiseAvailable && input.sprint && move.y >= -0.15f;
            IsCruising = cruiseThrust && cruiseFullSpeed;
            bool fast = rb.linearVelocity.sqrMagnitude > stats.SprintSpeed * stats.SprintSpeed * 1.21f;
            Vector3 localThrust = new Vector3(move.x, 0f, move.y);
            float targetSpeed = input.sprint ? stats.SprintSpeed : stats.Speed;
            float responseTime = move.sqrMagnitude > 0.001f ? thrustResponseTime : coastResponseTime;

            if (cruiseThrust)
            {
                localThrust = new Vector3(move.x * 0.35f, 0f, 1f).normalized;
                targetSpeed = IsCruising ? cruiseSpeed : stats.SprintSpeed;
                responseTime = IsCruising ? cruiseResponseTime : fast ? cruiseBrakeTime : thrustResponseTime;
            }
            else if (fast)
            {
                responseTime = cruiseBrakeTime;
            }

            CollisionDetectionMode collisionMode = IsCruising || fast
                ? CollisionDetectionMode.ContinuousDynamic
                : normalCollisionMode;
            if (rb.collisionDetectionMode != collisionMode) rb.collisionDetectionMode = collisionMode;

            Vector3 targetVelocity = viewRotation * (localThrust * targetSpeed);
            rb.linearVelocity = Vector3.SmoothDamp(rb.linearVelocity, targetVelocity, ref thrustRate,
                responseTime, Mathf.Infinity, dt);
        }

        private void UpdateSteering()
        {
            // O controle da ação fornece deltas em pixels ou a posição do analógico antes
            // dos multiplicadores do controle por torque usado nas cenas antigas.
            var lookControl = lookAction?.activeControl as InputControl<Vector2>;
            Vector2 look = lookControl?.ReadValue() ?? (lookAction == null ? input.look : Vector2.zero);
            bool isStick = lookControl?.device is Gamepad;
            Vector2 stick = isStick ? look : Vector2.zero;
            Vector2 mouse = isStick ? Vector2.zero : look * angleSpeedMultiplier;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            float blend = 1f - Mathf.Exp(-dt / turnResponseTime);
            Vector3 requestedRate = new Vector3(-stick.y * stickTurnRate, stick.x * stickTurnRate,
                -input.roll.x * (rotationSpeedMultiplier * 650f));
            angularInputRate = Vector3.Lerp(angularInputRate, requestedRate, blend);
            Vector3 turn = angularInputRate * dt + new Vector3(-mouse.y, mouse.x, 0f);
            if (turn.sqrMagnitude > 0.0000001f)
                viewRotation = (viewRotation * Quaternion.Euler(turn)).normalized;
        }

        public void SetCruiseAvailable(bool available, bool fullSpeed = true)
        {
            cruiseAvailable = available;
            cruiseFullSpeed = fullSpeed;
            if (!available || !fullSpeed) IsCruising = false;
        }

        public void TakeDamage(float damage) => TakeDamage(damage, null);

        public void TakeDamage(float damage, Vector3? sourcePosition)
        {
            if (!IsAlive || !float.IsFinite(damage) || damage <= 0f || Time.time < nextDamageTime) return;

            nextDamageTime = Time.time + hitImmunity;
            float appliedDamage = Mathf.Min(Health, damage);
            Health -= appliedDamage;
            if (!IsAlive)
            {
                SetCruiseAvailable(false);
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            Damaged?.Invoke(appliedDamage, sourcePosition);
        }

        public void RestoreHealth(float amount)
        {
            if (IsAlive && amount > 0f)
            {
                Health = Mathf.Min(maxHealth, Health + amount);
            }
        }

        public void CollectGem()
        {
            GemsCollected++;
            stats.Experience += stats.IncrementExperience;
        }

        private void Update()
        {
            if (!IsAlive || !input.cursorInputForLook || Time.timeScale == 0f)
            {
                angularInputRate = Vector3.zero;
                return;
            }

            UpdateSteering();

            float requiredExperience = stats.MaxExperience;
            if (stats.Experience < requiredExperience || UIManager.Instance == null ||
                !UIManager.Instance.TryShowLevelUp(input)) return;

            angularInputRate = Vector3.zero;
            stats.MaxExperience = Mathf.Ceil(requiredExperience * 1.18f);
            stats.Experience -= requiredExperience;
        }
    }
}
