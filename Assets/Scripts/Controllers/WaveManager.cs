using System;
using System.Collections;
using System.Collections.Generic;
using Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

[DefaultExecutionOrder(220)]
public class WaveManager : MonoBehaviour
{
    [Serializable]
    private struct EnemySource
    {
        public EnemiesConfig config;
        public PoolID pool;
    }

    [Serializable]
    private struct Assault
    {
        [Min(0)] public int drones;
        [Min(0)] public int rammers;
        [Min(0)] public int sentinels;
        [Min(0.1f)] public float secondsBetweenSpawns;

        public int Count => drones + rammers + sentinels;
    }

    [Serializable]
    private class Sector
    {
        public string title;
        public string briefing;
        public Vector3 entryOffset;
        public Color signalColor = Color.cyan;
        public Color sunlight = Color.white;
        public MarkSectorScenery.Layout scenery;
        [Min(0.1f)] public float enemyHealth = 1f;
        [Min(0.1f)] public float enemySpeed = 1f;
        [Min(0.1f)] public float enemyDamage = 1f;
        [Range(15f, 180f)] public float spawnArc = 55f;
        public Assault[] assaults;
        public EnemiesConfig boss;
    }

    private enum NavigationLeg { None, Satellite, Cruise, Approach }

    private const int MaxActiveEnemies = 15;
    private const float RelayLeadDistance = 62f;
    private const float SceneryLoadDistance = 265f;
    private const float SceneryUnloadDistance = 325f;
    private const float CruiseBrakeDistance = 150f;
    private const float SectorArrivalDistance = 95f;
    private static readonly WaitForSeconds WaveBreak = new WaitForSeconds(2.8f);
    private static readonly Color LaraSignal = new(0.43f, 0.93f, 0.87f);
    private static readonly Color HermesSignal = new(1f, 0.55f, 0.71f);

    [SerializeField] private EnemySource drone;
    [SerializeField] private EnemySource rammer;
    [SerializeField] private EnemySource sentinel;
    [SerializeField] private Sector[] sectors;
    [SerializeField] private Canvas hudCanvas;
    [SerializeField] private Light sun;
    [SerializeField] private Shader signalShader;
    [SerializeField] private AudioClip hullHitClip;

    private readonly List<GameObject> activeEnemies = new List<GameObject>(MaxActiveEnemies);
    private NaveController player;
    private StarterAssetsInputs playerInput;
    private Camera pilotCamera;
    private MarkCampaignHud hud;
    private MarkSectorScenery scenery;
    private OrbitalTransferPresentation transfer;
    private AudioSource hullAudio;
    private Coroutine campaign;
    private EnemySystem boss;
    private CoreTerminal terminal;
    private RouteSatellite routeSatellite;
    private NavigationLeg navigationLeg;
    private Vector3 navigationTarget;
    private bool finished;
    private bool victory;
    private bool outcomeShown;
    private Vector3 firstEntry;
    private Quaternion firstHeading;
    private int sectorNumber;
    private int waveNumber;
    private int unspawnedEnemies;

    public static WaveManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartNextWave();
    }

    public void StartNextWave()
    {
        if (campaign != null || finished) return;
        player = FindFirstObjectByType<NaveController>();
        if (player == null || sectors == null || sectors.Length == 0) return;

        playerInput = player.GetComponent<StarterAssetsInputs>();
        pilotCamera = Camera.main;
        firstEntry = player.transform.position;
        firstHeading = player.transform.rotation;
        if (hudCanvas != null) hud = new MarkCampaignHud(hudCanvas);
        scenery = gameObject.AddComponent<MarkSectorScenery>();
        transfer = gameObject.AddComponent<OrbitalTransferPresentation>();
        if (hullHitClip != null)
        {
            hullAudio = gameObject.AddComponent<AudioSource>();
            hullAudio.playOnAwake = false;
            hullAudio.outputAudioMixerGroup = player.GetComponent<AudioSource>()?.outputAudioMixerGroup;
        }
        player.Damaged += OnHullDamaged;
        campaign = StartCoroutine(PlayCampaign());
    }

    private void Update()
    {
        if (player == null) return;

        PruneInactiveEnemies();
        hud?.TickPresentation();
        if (!finished && !player.IsAlive) Finish(false);

        if (finished)
        {
            ShowOutcome();
            return;
        }

        scenery.UpdateVisibility(player.transform.position);
        int activeCount = activeEnemies.Count + (boss != null && boss.IsAlive ? 1 : 0);
        hud?.SetStatus(Mathf.CeilToInt(player.Health), Mathf.CeilToInt(player.MaxHealth),
            player.GemsCollected, unspawnedEnemies + activeCount);
        if (boss != null && boss.IsAlive)
        {
            hud?.SetBossStatus(boss.HealthRatio, boss.IsTerminalVulnerable,
                terminal != null && terminal.CanFocus(player.transform));
            if (pilotCamera != null)
                hud?.PointToBoss(pilotCamera.WorldToViewportPoint(boss.AimPoint));
        }
    }

    private void OnHullDamaged(float damage, Vector3? sourcePosition)
    {
        Vector2 bearing = Vector2.zero;
        if (sourcePosition.HasValue && pilotCamera != null)
        {
            Vector3 incident = pilotCamera.transform.InverseTransformDirection(sourcePosition.Value - player.transform.position);
            bearing = new Vector2(incident.x, incident.y);
            if (bearing.sqrMagnitude < incident.sqrMagnitude * 0.04f)
                bearing = incident.z >= 0f ? Vector2.up : Vector2.down;
        }

        hud?.ShowHullImpact(damage, bearing);
        if (hullAudio != null)
        {
            hullAudio.pitch = Mathf.Lerp(1.05f, 0.85f, Mathf.InverseLerp(5f, 25f, damage));
            hullAudio.PlayOneShot(hullHitClip, 0.78f);
        }
        CinemachineShake.Instance?.ShakeCamera(Mathf.Lerp(0.28f, 0.43f, Mathf.InverseLerp(5f, 23f, damage)), 0.21f);
    }

    private void LateUpdate()
    {
        if (navigationLeg == NavigationLeg.None || hud == null || player == null) return;

        string hint = navigationLeg switch
        {
            NavigationLeg.Satellite => routeSatellite != null && routeSatellite.CanFocus(player.transform)
                ? "Espaço / A para acessar o terminal"
                : "Aproxime-se para decodificar as coordenadas",
            NavigationLeg.Cruise => "Segure Shift / LT para cruzeiro",
            _ => "Aproximação · Impulso reduzido"
        };
        hud.UpdateNavigation(hint, Vector3.Distance(player.transform.position, navigationTarget), player.TravelSpeed);
        if (pilotCamera != null) hud.PointToNavigation(pilotCamera, navigationTarget);
    }

    private IEnumerator PlayCampaign()
    {
        for (int i = 0; i < sectors.Length; i++)
        {
            Sector sector = sectors[i];
            if (i == 0)
                scenery.Enter(sector.scenery, firstEntry, firstHeading, sector.signalColor, signalShader);
            else
            {
                yield return TravelToSector(sectors[i - 1], sector, i + 1);
                player.RestoreHealth(player.MaxHealth * 0.28f);
            }

            sectorNumber = i + 1;
            waveNumber = 0;

            if (sun != null) sun.color = sector.sunlight;
            hud?.EnterSector(sectorNumber, sectors.Length, sector.title, sector.briefing, sector.signalColor);
            if (sectorNumber == 1)
                Lara("A H.E.R.M.E.S. apagou os canais de Mark. Ainda consigo ouvir os pulsares no rádio.", 5.5f);
            yield return new WaitForSeconds(3f);

            foreach (Assault assault in sector.assaults)
            {
                waveNumber++;
                int waveTotal = sector.assaults.Length + (sector.boss != null ? 1 : 0);
                unspawnedEnemies = assault.Count;
                hud?.SetWave(waveNumber, waveTotal);
                yield return SpawnAssault(sector, assault);
                Projectile.Instance?.Clear();

                if (waveNumber < waveTotal)
                {
                    hud?.ShowNotice("Onda concluída", 3.2f);
                    yield return WaveBreak;
                }
            }

            if (sector.boss != null)
            {
                waveNumber++;
                hud?.SetWave(waveNumber, waveNumber);
                if (!SpawnBoss(sector.boss))
                {
                    yield break;
                }

                Hermes("Você me ensinou a detectar o que estava errado, Lara. A lista de evacuação tinha nomes reservados.", 6.5f);
                yield return new WaitUntil(() => boss == null || !boss.IsAlive);
                Projectile.Instance?.Clear();
                hud?.HideBoss();
            }
        }

        campaign = null;
        Finish(true);
    }

    private IEnumerator TravelToSector(Sector origin, Sector destination, int destinationNumber)
    {
        Vector3 nextEntry = firstEntry + destination.entryOffset;
        Vector3 toDestination = nextEntry - player.transform.position;
        Vector3 relayDirection = toDestination.sqrMagnitude > 1f ? toDestination.normalized : player.transform.forward;
        navigationTarget = player.transform.position + relayDirection * RelayLeadDistance;
        Vector3? decodedArrival = null;
        routeSatellite = RouteSatellite.Attach(navigationTarget, transform, destinationNumber, nextEntry,
            coordinates => decodedArrival = coordinates, signalShader, hudCanvas);
        navigationLeg = NavigationLeg.Satellite;
        hud?.BeginNavigation(destinationNumber == 2 ? "Retransmissor de Mark" : "Estação de triangulação", origin.signalColor);
        if (destinationNumber == 2)
            Lara("O pulso está preso no retransmissor. Vou restaurar o filtro e seguir o sinal.", 6f);
        else
            Hermes("Você encontrou meus pulsos. Para chegar ao núcleo, ainda precisa atravessar a tempestade.", 6.5f);
        yield return new WaitUntil(() => decodedArrival.HasValue);

        nextEntry = decodedArrival.Value;
        navigationTarget = nextEntry;
        hud?.BeginNavigation(destination.title, destination.signalColor);
        transfer.Begin(player.transform, pilotCamera, nextEntry, destination.signalColor);
        if (destinationNumber == 2)
            Lara("Pulso recuperado. A assinatura do canal de manutenção ecoa no cinturão de detritos.", 6f);
        else
            Lara("Três pulsos, uma posição. A janela na tempestade leva direto ao núcleo da H.E.R.M.E.S.", 6f);

        Vector3 course = nextEntry - (firstEntry + origin.entryOffset);
        Quaternion heading = course.sqrMagnitude > 0.01f ? Quaternion.LookRotation(course) : firstHeading;
        bool destinationLoaded = false;
        bool arrivalAnnounced = false;
        while (true)
        {
            float distance = Vector3.Distance(player.transform.position, nextEntry);
            float approach = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(SceneryLoadDistance, SectorArrivalDistance, distance));
            if (sun != null) sun.color = Color.Lerp(origin.sunlight, destination.sunlight, approach);
            if (!destinationLoaded && distance <= SceneryLoadDistance)
            {
                scenery.Enter(destination.scenery, nextEntry, heading, destination.signalColor, signalShader);
                destinationLoaded = true;

                if (!arrivalAnnounced)
                {
                    arrivalAnnounced = true;
                    if (destinationNumber == 2)
                        Lara("Detritos no radar. O retransmissor seguinte está escondido no cinturão.", 5f);
                    else
                        Hermes("Sua nave está entrando na janela de Mark. O núcleo já detectou sua assinatura.", 5f);
                }
            }
            else if (destinationLoaded && distance >= SceneryUnloadDistance)
            {
                scenery.Clear();
                destinationLoaded = false;
            }

            bool fullSpeed = distance > CruiseBrakeDistance;
            player.SetCruiseAvailable(true, fullSpeed);
            navigationLeg = fullSpeed ? NavigationLeg.Cruise : NavigationLeg.Approach;
            transfer.UpdateFlight(player.TravelSpeed, distance, player.IsCruising);
            if (distance <= SectorArrivalDistance) break;
            yield return null;
        }

        EndNavigation();
    }

    private void Lara(string message, float seconds) => hud?.Transmit("LARA / 14-BIS II", message, LaraSignal, seconds);

    private void Hermes(string message, float seconds) => hud?.Transmit("H.E.R.M.E.S. / SINAL INTERCEPTADO", message, HermesSignal, seconds);

    private IEnumerator SpawnAssault(Sector sector, Assault assault)
    {
        int drones = assault.drones;
        int rammers = assault.rammers;
        int groupSize = sectorNumber == 1 && waveNumber == 1 ? 4 : 5;
        while (unspawnedEnemies > 0)
        {
            if (activeEnemies.Count >= MaxActiveEnemies)
                yield return new WaitUntil(() => activeEnemies.Count < MaxActiveEnemies);

            int count = Mathf.Min(groupSize, unspawnedEnemies, MaxActiveEnemies - activeEnemies.Count);
            for (int member = 0; member < count; member++)
            {
                int selection = Random.Range(0, unspawnedEnemies);
                EnemySource source;
                if (selection < drones)
                {
                    source = drone;
                    drones--;
                }
                else if (selection < drones + rammers)
                {
                    source = rammer;
                    rammers--;
                }
                else
                {
                    source = sentinel;
                }

                SpawnDrone(source, sector, member, count);
                unspawnedEnemies--;
            }

            groupSize = 3;
            yield return new WaitForSeconds(Mathf.Max(0.1f, assault.secondsBetweenSpawns));
        }

        yield return new WaitUntil(() => activeEnemies.Count == 0);
    }

    private void SpawnDrone(EnemySource source, Sector sector, int member, int count)
    {
        if (source.config == null || source.config.enemyPrefab == null)
        {
            Debug.LogError("Configure um prefab para cada inimigo das ondas de Mark.", this);
            return;
        }

        Transform ship = player.transform;
        Vector3 position = FindSpawnPosition(ship, sector.spawnArc, member, count);
        Quaternion rotation = Quaternion.LookRotation(ship.position - position);
        GameObject enemy = source.pool != null && ObjectPool.Instance != null
            ? ObjectPool.Instance.GetInstance(source.pool)
            : null;
        if (enemy == null)
            enemy = Instantiate(source.config.enemyPrefab, position, rotation);

        if (!enemy.TryGetComponent(out EnemySystem system))
        {
            enemy.SetActive(false);
            Debug.LogError("O prefab da onda precisa de um EnemySystem.", enemy);
            return;
        }

        enemy.transform.SetPositionAndRotation(position, rotation);
        system.Configure(source.config, ship, sector.enemyHealth, sector.enemySpeed, sector.enemyDamage);
        enemy.SetActive(true);
        activeEnemies.Add(enemy);
        ShipManager.Instance?.AddActiveShip(enemy.transform);
    }

    private Vector3 FindSpawnPosition(Transform ship, float arc, int member, int count)
    {
        float lane = 2f * (member + 0.5f) / count - 1f;
        Vector3 position = ship.position + ship.forward * 82f;
        for (int attempt = 0; attempt < 4; attempt++)
        {
            float distance = Random.Range(64f, 96f);
            if (pilotCamera != null)
            {
                float screenSpread = Mathf.Lerp(0.27f, 0.37f, Mathf.InverseLerp(40f, 117f, arc));
                float shipDepth = pilotCamera.WorldToViewportPoint(ship.position).z;
                float depth = Mathf.Max(0f, shipDepth) + distance;
                position = pilotCamera.ViewportToWorldPoint(new Vector3(0.5f + lane * screenSpread +
                    Random.Range(-0.1f, 0.1f), Random.Range(0.32f, 0.70f), depth));
            }
            else
            {
                Vector3 direction = Quaternion.AngleAxis(lane * Mathf.Min(arc, 38f), ship.up) * ship.forward;
                position = ship.position + direction * distance + ship.up * Random.Range(-14f, 14f);
            }

            if (!Physics.CheckSphere(position, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
                break;
        }
        return position;
    }

    private bool SpawnBoss(EnemiesConfig config)
    {
        if (config.enemyPrefab == null)
        {
            Debug.LogError("Configure o prefab do núcleo H.E.R.M.E.S.", this);
            Finish(false);
            return false;
        }

        Transform ship = player.transform;
        Vector3 position = ship.position + ship.forward * 92f;
        GameObject bossObject = Instantiate(config.enemyPrefab, position, Quaternion.LookRotation(ship.position - position));
        if (!bossObject.TryGetComponent(out boss))
        {
            Destroy(bossObject);
            Debug.LogError("O prefab do núcleo precisa de um EnemySystem.", this);
            Finish(false);
            return false;
        }

        boss.Configure(config, ship);
        terminal = CoreTerminal.Attach(boss, hud, signalShader);
        return true;
    }

    private void EndNavigation()
    {
        if (player != null) player.SetCruiseAvailable(false);
        transfer?.End();
        navigationLeg = NavigationLeg.None;
        hud?.EndNavigation();
        if (routeSatellite != null)
        {
            Destroy(routeSatellite.gameObject);
            routeSatellite = null;
        }
    }

    public void RegisterEnemyDeath()
    {
        PruneInactiveEnemies();
    }

    private void PruneInactiveEnemies()
    {
        int count = activeEnemies.Count;
        for (int i = count - 1; i >= 0; i--)
        {
            GameObject enemy = activeEnemies[i];
            if (enemy == null || !enemy.activeInHierarchy)
                activeEnemies.RemoveAt(i);
        }

        if (activeEnemies.Count != count)
            ShipManager.Instance?.RemoveDisabledShips();
    }

    private void Finish(bool won)
    {
        if (finished) return;
        finished = true;
        victory = won;
        EndNavigation();
        if (campaign != null)
        {
            StopCoroutine(campaign);
            campaign = null;
        }

        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null) enemy.SetActive(false);
        }

        PruneInactiveEnemies();
        if (boss != null && boss.IsAlive) boss.gameObject.SetActive(false);
        Projectile.Instance?.Clear();
        hud?.HideBoss();
        ShowOutcome();
    }

    private void ShowOutcome()
    {
        if (outcomeShown || hud == null || UIManager.Instance == null ||
            !UIManager.Instance.TryBeginTacticalPause(this, playerInput)) return;

        outcomeShown = true;
        hud.ShowOutcome(victory, player.GemsCollected, Restart);
    }

    private void Restart()
    {
        if (!outcomeShown) return;
        UIManager.Instance?.EndTacticalPause(this);
        SceneManager.LoadScene(gameObject.scene.buildIndex);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (player != null) player.Damaged -= OnHullDamaged;
        if (outcomeShown) UIManager.Instance?.EndTacticalPause(this);
        hud?.Dispose();
    }
}
