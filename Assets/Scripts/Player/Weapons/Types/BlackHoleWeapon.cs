using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Arma de área: a cada weaponData.attackCooldown spawna weaponData.projectileAmount objetos
/// da pool dentro de um cilindro centrado no player, priorizando onde há mais inimigos juntos.
/// </summary>
public class BlackHoleWeapon : WeaponBase
{
    [Header("Área Cilíndrica")]
    [Tooltip("Raio do cilindro (plano XZ)")]
    public float areaRadius = 10f;
    [Tooltip("Altura total do cilindro (metade para cima, metade para baixo do player)")]
    public float areaHeight = 4f;
    public LayerMask enemyLayer;

    [Header("Concentração de Inimigos")]
    [Tooltip("Raio usado para contar quantos inimigos estão 'juntos'")]
    public float clusterRadius = 3f;
    [Tooltip("Variação aleatória em torno do centro do grupo")]
    public float spawnScatter = 1f;
    [Tooltip("Se não houver inimigos, spawna em ponto aleatório da área")]
    public bool spawnWhenNoEnemies = false;

    [Header("Animação de Escala (Ease)")]
    [Tooltip("Tempo para crescer ao aparecer")]
    public float appearDuration = 0.25f;
    [Tooltip("Tempo para diminuir ao sumir (usa a mesma curva, espelhada)")]
    public float disappearDuration = 0.25f;
    [Tooltip("Curva de ease: eixo X = progresso (0 a 1), eixo Y = escala (0 a 1). Pode passar de 1 para dar overshoot.")]
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private class ActiveObject
    {
        public GameObject obj;
        public float elapsed;
        public float lifetime;
        public Vector3 baseScale;
    }

    private float timer;

    private readonly Collider[] hitBuffer = new Collider[256];
    private readonly List<Vector3> enemyPositions = new List<Vector3>(256);
    private readonly HashSet<IDamageable> seenEnemies = new HashSet<IDamageable>();
    private readonly List<ActiveObject> activeObjects = new List<ActiveObject>();
    // Escala original de cada objeto da pool (capturada na primeira vez que aparece)
    private readonly Dictionary<GameObject, Vector3> baseScales = new Dictionary<GameObject, Vector3>();

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        playerTransform = player;
        timer = 0f; // primeiro spawn imediato
    }

    void Update()
    {
        if (playerTransform == null || weaponData == null) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SpawnObjects();
            timer = weaponData.attackCooldown;
        }

        UpdateLifetimes();
    }

    // ---------------------------------------------------------------- Spawn

    private void SpawnObjects()
    {
        CollectEnemiesInCylinder();

        if (enemyPositions.Count == 0 && !spawnWhenNoEnemies) return;

        // Cópia de trabalho: inimigos de grupos já usados saem dela para espalhar os spawns
        List<Vector3> remaining = new List<Vector3>(enemyPositions);

        for (int i = 0; i < weaponData.projectileAmount; i++)
        {
            Vector3 spawnPos;

            if (enemyPositions.Count == 0)
            {
                spawnPos = RandomPointInCylinder();
            }
            else
            {
                // Se todos os grupos já foram usados, reaproveita a lista completa
                if (remaining.Count == 0) remaining.AddRange(enemyPositions);

                Vector3 center = FindDensestClusterCenter(remaining, out List<int> memberIndices);

                // Remove os membros do grupo escolhido (de trás pra frente)
                for (int m = memberIndices.Count - 1; m >= 0; m--)
                    remaining.RemoveAt(memberIndices[m]);

                Vector2 scatter = Random.insideUnitCircle * spawnScatter;
                spawnPos = center + new Vector3(scatter.x, 0f, scatter.y);
                spawnPos = ClampToCylinder(spawnPos);
            }

            GameObject obj = ObjectPool.Instance.GetInstance(weaponData.projectilePoolID);
            if (obj == null) continue;

            // Guarda a escala original só na primeira vez (evita capturar escala no meio da animação)
            if (!baseScales.TryGetValue(obj, out Vector3 baseScale))
            {
                baseScale = obj.transform.localScale;
                baseScales[obj] = baseScale;
            }

            activeObjects.RemoveAll(item => item.obj == obj);

            ActiveObject active = new ActiveObject
            {
                obj = obj,
                elapsed = 0f,
                lifetime = weaponData.lifetime,
                baseScale = baseScale
            };

            obj.transform.position = spawnPos;
            obj.transform.rotation = Quaternion.identity;
            ApplyScale(active); // escala inicial (0) antes de ativar, pra não piscar em tamanho cheio
            obj.SetActive(true);

            activeObjects.Add(active);
        }
    }

    /// <summary>
    /// Procura o inimigo com mais vizinhos dentro de clusterRadius e devolve o centro (média) do grupo.
    /// memberIndices = índices (ordenados) dos inimigos do grupo na lista recebida.
    /// </summary>
    private Vector3 FindDensestClusterCenter(List<Vector3> points, out List<int> memberIndices)
    {
        float sqrCluster = clusterRadius * clusterRadius;
        int bestCount = -1;
        int bestIndex = 0;

        for (int i = 0; i < points.Count; i++)
        {
            int count = 0;
            for (int j = 0; j < points.Count; j++)
            {
                if (SqrDistanceXZ(points[i], points[j]) <= sqrCluster) count++;
            }

            if (count > bestCount)
            {
                bestCount = count;
                bestIndex = i;
            }
        }

        memberIndices = new List<int>();
        Vector3 sum = Vector3.zero;

        for (int j = 0; j < points.Count; j++)
        {
            if (SqrDistanceXZ(points[bestIndex], points[j]) <= sqrCluster)
            {
                memberIndices.Add(j);
                sum += points[j];
            }
        }

        return sum / memberIndices.Count;
    }

    // ------------------------------------------------------- Detecção (cilindro)

    private void CollectEnemiesInCylinder()
    {
        enemyPositions.Clear();
        seenEnemies.Clear();

        Vector3 center = playerTransform.position;
        float halfHeight = areaHeight * 0.5f;

        // Esfera que envolve o cilindro; depois filtramos pelo formato exato
        float boundingRadius = Mathf.Sqrt(areaRadius * areaRadius + halfHeight * halfHeight);
        int count = Physics.OverlapSphereNonAlloc(center, boundingRadius, hitBuffer, enemyLayer);

        for (int i = 0; i < count; i++)
        {
            Collider col = hitBuffer[i];
            Vector3 p = col.transform.position;

            if (Mathf.Abs(p.y - center.y) > halfHeight) continue;
            if (SqrDistanceXZ(p, center) > areaRadius * areaRadius) continue;

            // Evita contar o mesmo inimigo várias vezes (vários colliders)
            IDamageable enemy = col.GetComponentInParent<IDamageable>();
            if (enemy != null && !seenEnemies.Add(enemy)) continue;

            enemyPositions.Add(p);
        }
    }

    private Vector3 RandomPointInCylinder()
    {
        Vector2 circle = Random.insideUnitCircle * areaRadius;
        Vector3 c = playerTransform.position;
        return new Vector3(c.x + circle.x, c.y, c.z + circle.y);
    }

    private Vector3 ClampToCylinder(Vector3 pos)
    {
        Vector3 c = playerTransform.position;
        Vector3 flat = new Vector3(pos.x - c.x, 0f, pos.z - c.z);

        if (flat.sqrMagnitude > areaRadius * areaRadius)
            flat = flat.normalized * areaRadius;

        float halfHeight = areaHeight * 0.5f;
        float y = Mathf.Clamp(pos.y, c.y - halfHeight, c.y + halfHeight);

        return new Vector3(c.x + flat.x, y, c.z + flat.z);
    }

    private static float SqrDistanceXZ(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }

    // ------------------------------------------------------------ Lifetime

    private void UpdateLifetimes()
    {
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            ActiveObject item = activeObjects[i];

            if (item.obj == null)
            {
                activeObjects.RemoveAt(i);
                continue;
            }

            item.elapsed += Time.deltaTime;

            if (item.elapsed >= item.lifetime)
            {
                // Devolve a escala original para o próximo uso na pool
                item.obj.transform.localScale = item.baseScale;
                item.obj.SetActive(false);
                activeObjects.RemoveAt(i);
                continue;
            }

            ApplyScale(item);
        }
    }

    /// <summary>
    /// Escala = ease de entrada * ease de saída (a saída é a mesma curva espelhada no tempo).
    /// </summary>
    private void ApplyScale(ActiveObject item)
    {
        float appearT = appearDuration > 0f ? Mathf.Clamp01(item.elapsed / appearDuration) : 1f;
        float disappearT = disappearDuration > 0f
            ? Mathf.Clamp01((item.lifetime - item.elapsed) / disappearDuration)
            : 1f;

        float factor = easeCurve.Evaluate(appearT) * easeCurve.Evaluate(disappearT);
        item.obj.transform.localScale = item.baseScale * Mathf.Max(0f, factor);
    }

    // ------------------------------------------------------------- Gizmos

    void OnDrawGizmosSelected()
    {
        Transform t = playerTransform != null ? playerTransform : transform;
        Vector3 c = t.position;
        float h = areaHeight * 0.5f;

        Gizmos.color = Color.cyan;
        DrawCircle(c + Vector3.up * h, areaRadius);
        DrawCircle(c - Vector3.up * h, areaRadius);

        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.PI * 0.5f;
            Vector3 off = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * areaRadius;
            Gizmos.DrawLine(c + off + Vector3.up * h, c + off - Vector3.up * h);
        }
    }

    private static void DrawCircle(Vector3 center, float radius, int segments = 32)
    {
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            Vector3 next = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}