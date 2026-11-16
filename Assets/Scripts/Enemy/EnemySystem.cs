using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySystem : MonoBehaviour, IDamageable
{
    private const float PlayerHitRadius = 1.9f;
    private const float ChargeDuration = 0.52f;
    private const float TerminalWindow = 5.5f;
    private const float CoreShieldDuration = 4.5f;
    private const int DecisionFrameCount = 3;
    private const float FormationSpacing = 9f;

    private static readonly List<EnemySystem> activeEnemies = new List<EnemySystem>();
    private static int nextDecisionFrame;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly Color AttackTellColor = new Color(1f, 0.62f, 0.13f);
    private static readonly Color ExposedColor = new Color(0.19f, 0.97f, 0.87f);
    private static readonly Color ShieldColor = new Color(0.55f, 0.37f, 0.95f);

    public EnemiesConfig config;
    public PoolID gemPool;
    [Min(0f)] public float combineTimeWindow = 0.1f;
    public Transform popupSpawnPoint;

    private EnemiesConfig profile;
    private Transform player;
    private AudioSource playerAudio;
    private IDamageable playerHealth;
    private Collider[] hitColliders;
    private Renderer[] shipRenderers;
    private MaterialPropertyBlock appearance;
    private Color? currentTint;
    private DamagePopup lastPopup;
    private float lastDamageTime;
    private bool preparedForActivation;
    private float life;
    private float maxLife;
    private float pace;
    private float damage;
    private float knockbackResponse;
    private float attackTimer;
    private float windupRemaining;
    private float chargeRemaining;
    private float terminalCycle;
    private float hitFlashUntil;
    private int spreadIndex;
    private int decisionFrame;
    private Vector3 chargeDirection;
    private Vector3 moveVelocity;
    private Vector3 knockbackVelocity;
    private Vector3 separationVelocity;

    public static IReadOnlyList<EnemySystem> ActiveEnemies => activeEnemies;
    public bool IsAlive => isActiveAndEnabled && life > 0f;
    public bool CanTakeDamage => IsAlive && !CoreAtThreshold;
    public float HealthRatio => maxLife > 0f ? life / maxLife : 0f;
    public Vector3 Velocity { get; private set; }
    public bool IsTerminalVulnerable => IsAlive && CoreAtThreshold && terminalCycle < TerminalWindow;

    private bool IsBoss => profile != null && profile.enemyType == EnemyType.Boss;
    private bool CoreAtThreshold => IsBoss && life <= maxLife * 0.2f;

    public Vector3 AimPoint
    {
        get
        {
            if (hitColliders != null && hitColliders.Length > 0 && hitColliders[0].enabled)
            {
                Collider hitCollider = hitColliders[0];
                if (hitCollider is SphereCollider sphere) return sphere.transform.TransformPoint(sphere.center);
                if (hitCollider is CapsuleCollider capsule) return capsule.transform.TransformPoint(capsule.center);
                return hitCollider.bounds.center;
            }

            return transform.position;
        }
    }

    private void Awake()
    {
        hitColliders = GetComponentsInChildren<Collider>(true);
        shipRenderers = GetComponentsInChildren<Renderer>(true);
        appearance = new MaterialPropertyBlock();
        knockbackResponse = TryGetComponent(out Rigidbody body) ? Mathf.Min(1f, 18f / body.mass) : 1f;
    }

    private void OnEnable()
    {
        if (!preparedForActivation)
            Configure(config, GameObject.FindGameObjectWithTag("Player")?.transform);

        preparedForActivation = false;
        decisionFrame = nextDecisionFrame;
        nextDecisionFrame = (nextDecisionFrame + 1) % DecisionFrameCount;
        foreach (Collider hitCollider in hitColliders) hitCollider.enabled = true;
        activeEnemies.Add(this);
    }

    private void OnDisable()
    {
        activeEnemies.Remove(this);
        ShipManager.Instance?.RemoveActiveShip(transform);
        preparedForActivation = false;
    }

    public void Configure(EnemiesConfig spawnConfig, Transform playerTarget, float healthMultiplier = 1f,
        float speedMultiplier = 1f, float damageMultiplier = 1f)
    {
        profile = spawnConfig;
        player = playerTarget;
        playerHealth = player != null ? player.GetComponent<IDamageable>() : null;
        playerAudio = player != null ? player.GetComponent<AudioSource>() : null;
        preparedForActivation = !isActiveAndEnabled;

        maxLife = profile != null ? Mathf.Max(1f, profile.maxHealth * healthMultiplier) : 0f;
        pace = profile != null ? Mathf.Max(0f, profile.speed * speedMultiplier) : 0f;
        damage = profile != null ? Mathf.Max(0f, profile.damage * damageMultiplier) : 0f;
        life = maxLife;
        attackTimer = IsBoss ? Random.Range(0.55f, 1.35f) : Random.Range(0.85f, 2.1f);
        windupRemaining = 0f;
        chargeRemaining = 0f;
        terminalCycle = 0f;
        hitFlashUntil = 0f;
        spreadIndex = 0;
        moveVelocity = Vector3.zero;
        knockbackVelocity = Vector3.zero;
        separationVelocity = Vector3.zero;
        Velocity = Vector3.zero;
        lastPopup = null;
        lastDamageTime = float.NegativeInfinity;
        SetTint(null);
    }

    private void Update()
    {
        float step = Time.deltaTime;
        if (!IsAlive || player == null || step <= 0f) return;

        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;
        Vector3 direction = distance > 0.001f ? toPlayer / distance : transform.forward;
        if (IsBoss)
        {
            if (CoreAtThreshold)
                terminalCycle = Mathf.Repeat(terminalCycle + step, TerminalWindow + CoreShieldDuration);

            if (IsTerminalVulnerable)
                windupRemaining = 0f;
            else
                UpdateRangedAttack(distance, direction, step, true);
        }
        else
        {
            UpdateMovement(distance, direction, step, Time.frameCount % DecisionFrameCount == decisionFrame);
        }

        Vector3 facing = chargeRemaining > 0f ? chargeDirection : direction;
        if (IsBoss) facing.y = 0f;
        if (facing.sqrMagnitude > 0.001f)
        {
            Quaternion rotation = Quaternion.LookRotation(facing);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, 260f * step);
        }

        Color? tint = null;
        if (Time.unscaledTime < hitFlashUntil)
            tint = Color.white;
        else if (IsTerminalVulnerable)
            tint = ExposedColor;
        else if (windupRemaining > 0f)
            tint = AttackTellColor;
        else if (CoreAtThreshold)
            tint = ShieldColor;

        SetTint(tint);
    }

    private void UpdateMovement(float distance, Vector3 direction, float step, bool decide)
    {
        bool isRam = profile.enemyType == EnemyType.Fast;
        bool charging = chargeRemaining > 0f;
        Vector3 desiredVelocity;
        if (isRam)
        {
            desiredVelocity = GetRamVelocity(distance, direction, step, decide);
            charging |= chargeRemaining > 0f;
        }
        else
        {
            float chase = Mathf.Clamp01((distance - profile.distanceToStop) / 9f);
            float backUp = profile.enemyType == EnemyType.Tank
                ? Mathf.Clamp01((profile.distanceToStop - distance - 5f) / 12f) * 0.55f
                : 0f;
            desiredVelocity = direction * (pace * (chase - backUp));
            UpdateRangedAttack(distance, direction, step, decide);
        }

        // A formação decide em turnos; movimento, investidas e colisões continuam a cada quadro.
        if (decide) separationVelocity = CalculateSeparation();
        if (!charging) desiredVelocity += separationVelocity;

        moveVelocity = Vector3.MoveTowards(moveVelocity, desiredVelocity, step * pace * (isRam ? 8f : 5f));
        knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero, step * 90f);
        Velocity = moveVelocity + knockbackVelocity;
        Vector3 position = transform.position;
        Vector3 nextPosition = position + Velocity * step;
        transform.position = nextPosition;

        if (charging && playerHealth != null &&
            Projectile.CrossesPlayer(position - player.position, nextPosition - player.position, PlayerHitRadius + 2.1f))
        {
            chargeRemaining = 0f;
            moveVelocity *= 0.2f;
            if (playerHealth is Player.NaveController pilot)
                pilot.TakeDamage(damage, position);
            else
                playerHealth.TakeDamage(damage);
        }
    }

    private Vector3 CalculateSeparation()
    {
        Vector3 separation = Vector3.zero;
        Vector3 position = transform.position;
        float spacingSquared = FormationSpacing * FormationSpacing;
        foreach (EnemySystem neighbor in activeEnemies)
        {
            if (neighbor == this || !neighbor.IsAlive) continue;

            Vector3 offset = position - neighbor.transform.position;
            float squaredDistance = offset.sqrMagnitude;
            if (squaredDistance <= 0.001f || squaredDistance >= spacingSquared) continue;

            float distance = Mathf.Sqrt(squaredDistance);
            separation += offset / distance * (1f - distance / FormationSpacing);
        }

        return Vector3.ClampMagnitude(separation, 1f) * (pace * 0.22f);
    }

    private Vector3 GetRamVelocity(float distance, Vector3 direction, float step, bool decide)
    {
        attackTimer -= step;
        if (chargeRemaining > 0f)
        {
            chargeRemaining = Mathf.Max(0f, chargeRemaining - step);
            return chargeDirection * (pace * 1.95f);
        }

        if (windupRemaining > 0f)
        {
            windupRemaining -= step;
            if (windupRemaining <= 0f)
            {
                chargeDirection = direction;
                chargeRemaining = ChargeDuration;
                attackTimer = Mathf.Max(ChargeDuration, profile.attackCooldown);
                return chargeDirection * (pace * 1.95f);
            }

            return direction * (pace * 0.15f);
        }

        if (decide && attackTimer <= 0f && distance <= profile.attackRange)
        {
            windupRemaining = Mathf.Max(0.01f, profile.windupTime);
            return direction * (pace * 0.15f);
        }

        // Gives the ship a moment to disengage before its next pass.
        float retreat = attackTimer > 0f ? Mathf.Clamp01((10f - distance) / 8f) : 0f;
        float approach = Mathf.Clamp01((distance - 5f) / 5f) * (1f - retreat);
        return direction * (pace * (approach - retreat * 0.55f));
    }

    private void UpdateRangedAttack(float distance, Vector3 direction, float step, bool decide)
    {
        attackTimer -= step;
        if (windupRemaining > 0f)
        {
            windupRemaining -= step;
            if (windupRemaining <= 0f)
            {
                if (distance <= profile.attackRange * 1.15f)
                    Fire(direction);

                attackTimer = Mathf.Max(0.1f, profile.attackCooldown) * Random.Range(0.9f, 1.15f);
            }
        }
        else if (decide && attackTimer <= 0f && distance <= profile.attackRange && profile.bulletPool != null)
        {
            windupRemaining = Mathf.Max(0.01f, profile.windupTime);
        }
    }

    private void Fire(Vector3 direction)
    {
        if (damage <= 0f || Projectile.Instance == null || ObjectPool.Instance == null) return;

        if (!IsBoss)
        {
            bool guided = profile.projectileTurnSpeed > 0f;
            FireShot(direction, guided ? new Color(5f, 0.9f, 3.4f) : new Color(5f, 2.35f, 0.28f),
                guided ? 1.9f : 1.55f, profile.projectileTurnSpeed);
            return;
        }

        // Dois círculos intercalados com distâncias diferentes e lacunas entre os disparos.
        Vector3 right = Vector3.Cross(Mathf.Abs(direction.y) < 0.95f ? Vector3.up : Vector3.right, direction).normalized;
        Vector3 up = Vector3.Cross(direction, right);
        Color boltColor = new Color(7f, 0.6f, 1.8f);
        int lanes = spreadIndex % 2 == 0 ? 8 : 10;
        float spread = spreadIndex % 2 == 0 ? 0.105f : 0.22f;
        for (int lane = 0; lane < lanes; lane++)
        {
            float angle = (lane / (float)lanes + spreadIndex * 0.073f) * (2f * Mathf.PI);
            Vector3 offset = right * Mathf.Cos(angle) + up * Mathf.Sin(angle);
            FireShot((direction + spread * offset).normalized, boltColor, 2.35f, 0f);
        }

        FireShot(direction, boltColor, 2.35f, 0f);
        spreadIndex++;
    }

    private void FireShot(Vector3 direction, Color color, float size, float turnSpeed)
    {
        GameObject shot = ObjectPool.Instance.GetInstance(profile.bulletPool);
        if (shot == null || !shot.TryGetComponent(out EnemyBullet bullet)) return;

        shot.transform.SetPositionAndRotation(AimPoint + direction * (IsBoss ? 6.5f : 1.9f), Quaternion.LookRotation(direction));
        shot.SetActive(true);
        bullet.SetAppearance(color, size);
        Projectile.Instance.AddBullet(bullet, player, playerHealth, damage, profile.projectileSpeed,
            profile.attackRange, turnSpeed, PlayerHitRadius + size * 0.325f);
    }

    public void ApplyKnockback(Vector3 impulse)
    {
        if (!IsAlive || IsBoss) return;

        knockbackVelocity = Vector3.ClampMagnitude(knockbackVelocity + impulse * knockbackResponse, 55f);
    }

    public void TakeDamage(float amount)
    {
        if (!CanTakeDamage || amount <= 0f || !float.IsFinite(amount)) return;

        float previousLife = life;
        float minimumLife = IsBoss ? maxLife * 0.2f : 0f;
        life = Mathf.Max(minimumLife, life - amount);
        hitFlashUntil = Time.unscaledTime + 0.07f;
        SetTint(Color.white);
        ShowDamagePopup(previousLife - life);

        if (life <= 0f)
            Die();
        else if (CoreAtThreshold)
            terminalCycle = 0f;
    }

    public void OverloadCore()
    {
        if (!IsTerminalVulnerable) return;

        life = 0f;
        Die();
    }

    private void Die()
    {
        ShipManager.Instance?.RemoveActiveShip(transform);
        foreach (Collider hitCollider in hitColliders) hitCollider.enabled = false;
        if (playerAudio != null && profile.hitAudio != null)
            playerAudio.PlayOneShot(profile.hitAudio, 0.4f);

        if (gemPool != null && ObjectPool.Instance != null)
        {
            int reward = profile.enemyType == EnemyType.Tank ? 2 : 1;
            for (int i = 0; i < reward; i++)
            {
                GameObject gem = ObjectPool.Instance.GetInstance(gemPool);
                if (gem == null) continue;

                gem.transform.position = transform.position + transform.right * (i * 1.75f);
                gem.SetActive(true);
            }
        }

        bool heavy = profile.enemyType == EnemyType.Tank || IsBoss;
        if (heavy) UIManager.Instance?.PulseImpact();

        SetTint(Color.white);
        StartCoroutine(FinishDeath());
    }

    private IEnumerator FinishDeath()
    {
        yield return new WaitForSecondsRealtime(0.07f);
        gameObject.SetActive(false);
        WaveManager.Instance?.RegisterEnemyDeath();
    }

    private void ShowDamagePopup(float amount)
    {
        if (DamageManager.Instance == null) return;

        if (lastPopup != null && lastPopup.isActiveAndEnabled && Time.time <= lastDamageTime + combineTimeWindow)
        {
            lastPopup.AddDamage(amount);
        }
        else
        {
            Vector3 spawnPosition = popupSpawnPoint != null ? popupSpawnPoint.position : AimPoint;
            GameObject popup = DamageManager.Instance.ShowDamage(amount, spawnPosition);
            lastPopup = popup != null ? popup.GetComponent<DamagePopup>() : null;
        }

        lastDamageTime = Time.time;
    }

    private void SetTint(Color? tint)
    {
        if (currentTint == tint || shipRenderers == null) return;

        currentTint = tint;
        foreach (Renderer shipRenderer in shipRenderers)
        {
            if (shipRenderer == null) continue;

            if (tint.HasValue)
            {
                shipRenderer.GetPropertyBlock(appearance);
                appearance.SetColor(BaseColorId, tint.Value);
                appearance.SetColor(ColorId, tint.Value);
                shipRenderer.SetPropertyBlock(appearance);
            }
            else
            {
                shipRenderer.SetPropertyBlock(null);
            }
        }
    }
}
