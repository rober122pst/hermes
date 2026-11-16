using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(1000)]
internal sealed class OrbitalTransferPresentation : MonoBehaviour
{
    private const int MoteCount = 22;
    private const int RingSegments = 24;
    private const int MaxRibbons = MoteCount + RingSegments * 2 + 10;
    private const float ApproachVisibility = 265f;
    private static readonly Color EngineBlue = new Color(0.30f, 0.73f, 1f);
    private static readonly Color EngineCore = new Color(0.78f, 0.92f, 1f);

    private readonly Vector3[] positions = new Vector3[MaxRibbons * 4];
    private readonly Color[] colors = new Color[MaxRibbons * 4];
    private readonly int[] indices = new int[MaxRibbons * 6];
    private readonly Vector2[] moteDirections = new Vector2[MoteCount];
    private Transform ship;
    private Camera pilotCamera;
    private Vector3 destination;
    private Vector3 gateRight;
    private Vector3 gateUp;
    private Color routeColor;
    private Vector3 exhaustLocalPosition;
    private float exhaustSpacing;
    private float exhaustWidth;
    private MeshRenderer effectRenderer;
    private Mesh mesh;
    private Material material;
    private AudioSource engineSource;
    private AudioClip engineLoop;
    private Matrix4x4 worldToMesh;
    private bool presenting;
    private float speed;
    private float targetIntensity;
    private float intensity;
    private float targetApproach;
    private float approach;
    private float phase;
    private float elapsed;
    private int ribbonCount;

    public void Begin(Transform ship, Camera pilotCamera, Vector3 destination, Color accent)
    {
        End();
        if (ship == null || pilotCamera == null || !EnsureRenderer()) return;

        this.ship = ship;
        this.pilotCamera = pilotCamera;
        this.destination = destination;
        routeColor = Color.Lerp(EngineBlue, accent, 0.65f);
        Vector3 course = destination - ship.position;
        Vector3 normal = course.sqrMagnitude > 0.01f ? course.normalized : ship.forward;
        Vector3 up = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) < 0.95f ? Vector3.up : Vector3.forward;
        gateRight = Vector3.Cross(up, normal).normalized;
        gateUp = Vector3.Cross(normal, gateRight);
        SampleExhaustPlacement();
        AudioSource shipAudio = ship.GetComponent<AudioSource>();
        engineSource.outputAudioMixerGroup = shipAudio != null ? shipAudio.outputAudioMixerGroup : null;

        speed = 0f;
        phase = 0f;
        elapsed = 0f;
        intensity = 0f;
        approach = 0f;
        effectRenderer.enabled = false;
        presenting = true;
    }

    public void UpdateFlight(float speed, float distance, bool cruiseEngaged)
    {
        if (!presenting) return;

        this.speed = Mathf.Max(0f, speed);
        float thrust = Mathf.InverseLerp(16f, 96f, this.speed);
        targetIntensity = Mathf.SmoothStep(0f, 1f, thrust) * (cruiseEngaged ? 1f : 0.72f);
        targetApproach = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(ApproachVisibility, 212f, distance));
    }

    public void End()
    {
        presenting = false;
        targetIntensity = 0f;
        targetApproach = 0f;
    }

    private void LateUpdate()
    {
        if (effectRenderer == null) return;
        if (ship == null || pilotCamera == null)
        {
            effectRenderer.enabled = false;
            if (engineSource.isPlaying) engineSource.Stop();
            return;
        }

        float step = presenting ? Time.deltaTime : Time.unscaledDeltaTime;
        float blend = 1f - Mathf.Exp(-step * 7f);
        intensity = Mathf.Lerp(intensity, targetIntensity, blend);
        approach = Mathf.Lerp(approach, targetApproach, blend);
        UpdateEngineAudio();
        if (Mathf.Max(intensity, approach) < 0.008f)
        {
            effectRenderer.enabled = false;
            return;
        }

        elapsed += step;
        phase += step * (0.15f + speed * 0.009f);
        effectRenderer.transform.position = pilotCamera.transform.position;
        worldToMesh = effectRenderer.transform.worldToLocalMatrix;
        ribbonCount = 0;

        if (intensity > 0.018f) DrawExhaust();
        if (intensity > 0.10f) DrawPeripheralMotes();
        if (approach > 0.008f) DrawApproachBeacon();

        if (ribbonCount > 0)
        {
            mesh.SetVertices(positions);
            mesh.SetColors(colors);
            mesh.SetIndices(indices, 0, ribbonCount * 6, MeshTopology.Triangles, 0, false);
        }
        effectRenderer.enabled = ribbonCount > 0;
    }

    private void UpdateEngineAudio()
    {
        float targetVolume = presenting && Time.timeScale > 0f ? intensity * 0.18f : 0f;
        float blend = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 9f);
        engineSource.volume = Mathf.Lerp(engineSource.volume, targetVolume, blend);
        engineSource.pitch = Mathf.Lerp(0.83f, 1.13f, intensity);

        if (engineSource.volume >= 0.001f)
        {
            if (!engineSource.isPlaying) engineSource.Play();
        }
        else if (engineSource.isPlaying)
        {
            engineSource.Stop();
        }
    }

    private void DrawExhaust()
    {
        Color engineColor = Color.Lerp(EngineBlue, routeColor, 0.27f);
        float length = Mathf.Lerp(2f, 12f, intensity);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 root = ship.TransformPoint(exhaustLocalPosition + Vector3.right * (side * exhaustSpacing));
            Vector3 tail = root - ship.forward * (length * (0.96f + 0.04f * Mathf.Sin(elapsed * 5f + side)));
            AddRibbon(root, tail, exhaustWidth * 1.75f, exhaustWidth * 0.55f,
                WithAlpha(engineColor, 0.54f * intensity), WithAlpha(engineColor, 0f));
            AddRibbon(root, Vector3.Lerp(root, tail, 0.48f), exhaustWidth * 0.55f, exhaustWidth * 0.22f,
                WithAlpha(EngineCore, 0.74f * intensity), WithAlpha(engineColor, 0f));

            Vector3 glowExtent = pilotCamera.transform.up * (exhaustWidth * 1.8f);
            AddRibbon(root - glowExtent, root + glowExtent, exhaustWidth * 1.4f, exhaustWidth * 1.4f,
                WithAlpha(engineColor, intensity * 0.30f), WithAlpha(engineColor, intensity * 0.30f));
        }
    }

    private void DrawPeripheralMotes()
    {
        float fieldHeight = pilotCamera.orthographic ? pilotCamera.orthographicSize * 2f :
            Mathf.Tan(pilotCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2f;
        Color color = Color.Lerp(EngineCore, routeColor, 0.48f);
        for (int i = 0; i < MoteCount; i++)
        {
            float progress = Mathf.Repeat(phase * Mathf.Lerp(0.67f, 1.25f, Sample(i, 2)) + Sample(i, 3), 1f);
            float opacity = Mathf.Min(Mathf.InverseLerp(0.02f, 0.22f, progress),
                Mathf.InverseLerp(1f, 0.73f, progress));
            float radialPosition = Mathf.Lerp(0.71f, 1.08f, progress * progress);
            Vector2 head = Vector2.one * 0.5f + moteDirections[i] * radialPosition;
            Vector2 tail = head - moteDirections[i] * Mathf.Lerp(0.02f, 0.16f, intensity);
            float depth = Mathf.Lerp(19f, 47f, Sample(i, 4));
            float width = (pilotCamera.orthographic ? fieldHeight : fieldHeight * depth) * 0.0018f;
            float alpha = opacity * intensity * Mathf.Lerp(0.13f, 0.32f, Sample(i, 5));
            AddRibbon(pilotCamera.ViewportToWorldPoint(new Vector3(head.x, head.y, depth)),
                pilotCamera.ViewportToWorldPoint(new Vector3(tail.x, tail.y, depth)), width, width * 0.45f,
                WithAlpha(color, alpha), WithAlpha(color, 0f));
        }
    }

    private void DrawApproachBeacon()
    {
        for (int ring = 0; ring < 2; ring++)
        {
            float progress = Mathf.Repeat(elapsed * 0.52f + ring * 0.5f, 1f);
            float radius = Mathf.Lerp(10f, 23f, progress);
            Color tint = WithAlpha(routeColor, Mathf.Sin(progress * Mathf.PI) * approach * 0.76f);
            for (int i = 0; i < RingSegments; i++)
            {
                if (i % 6 == 5) continue;
                float angle = (i + ring * 0.5f) * (Mathf.PI * 2f / RingSegments);
                float next = angle + Mathf.PI * 2f / RingSegments;
                AddRibbon(GatePoint(angle, radius), GatePoint(next, radius), 0.27f, 0.27f, tint, tint);
            }
        }

        Color marker = WithAlpha(routeColor, approach * 0.54f);
        for (int i = 0; i < 4; i++)
        {
            float angle = (i + 0.5f) * (Mathf.PI * 0.5f);
            AddRibbon(GatePoint(angle, 25f), GatePoint(angle, 29f), 0.25f, 0.14f, marker, marker);
        }
    }

    private Vector3 GatePoint(float angle, float radius)
    {
        return destination + (gateRight * Mathf.Cos(angle) + gateUp * Mathf.Sin(angle)) * radius;
    }

    private void AddRibbon(Vector3 front, Vector3 back, float frontWidth, float backWidth, Color frontColor, Color backColor)
    {
        Vector3 view = pilotCamera.orthographic ? pilotCamera.transform.forward :
            (front + back) * 0.5f - pilotCamera.transform.position;
        Vector3 side = Vector3.Cross(view, back - front);
        side = side.sqrMagnitude > 0.0001f ? side.normalized : pilotCamera.transform.right;
        int index = ribbonCount++ * 4;
        positions[index] = worldToMesh.MultiplyPoint3x4(front - side * frontWidth);
        positions[index + 1] = worldToMesh.MultiplyPoint3x4(front + side * frontWidth);
        positions[index + 2] = worldToMesh.MultiplyPoint3x4(back + side * backWidth);
        positions[index + 3] = worldToMesh.MultiplyPoint3x4(back - side * backWidth);
        colors[index] = colors[index + 1] = frontColor;
        colors[index + 2] = colors[index + 3] = backColor;
    }

    private void SampleExhaustPlacement()
    {
        exhaustLocalPosition = new Vector3(0f, 0f, -1.15f);
        exhaustSpacing = 0.42f;
        exhaustWidth = 0.36f;
        float largestVolume = 0f;
        foreach (BoxCollider hull in ship.GetComponentsInChildren<BoxCollider>())
        {
            if (!hull.enabled || hull.isTrigger) continue;

            Vector3 scale = hull.transform.lossyScale;
            float volume = Mathf.Abs(hull.size.x * scale.x * hull.size.y * scale.y * hull.size.z * scale.z);
            if (volume <= largestVolume) continue;
            largestVolume = volume;
            Vector3 extentX = ship.InverseTransformVector(hull.transform.TransformVector(Vector3.right * (hull.size.x * 0.5f)));
            Vector3 extentY = ship.InverseTransformVector(hull.transform.TransformVector(Vector3.up * (hull.size.y * 0.5f)));
            Vector3 extentZ = ship.InverseTransformVector(hull.transform.TransformVector(Vector3.forward * (hull.size.z * 0.5f)));
            float width = Mathf.Abs(extentX.x) + Mathf.Abs(extentY.x) + Mathf.Abs(extentZ.x);
            float depth = Mathf.Abs(extentX.z) + Mathf.Abs(extentY.z) + Mathf.Abs(extentZ.z);
            exhaustLocalPosition = ship.InverseTransformPoint(hull.transform.TransformPoint(hull.center));
            exhaustLocalPosition.z -= depth;
            exhaustSpacing = Mathf.Clamp(width * 0.26f, 0.16f, 1.20f);
            exhaustWidth = Mathf.Clamp(width * 0.19f, 0.16f, 0.52f);
        }
    }

    private bool EnsureRenderer()
    {
        if (effectRenderer != null) return true;

        Shader shader = Resources.Load<Shader>("OrbitalTransfer");
        if (shader == null || !shader.isSupported) return false;

        var effect = new GameObject("Efeitos do cruzeiro orbital", typeof(MeshFilter), typeof(MeshRenderer));
        effect.layer = gameObject.layer;
        effect.transform.SetParent(transform, false);
        effectRenderer = effect.GetComponent<MeshRenderer>();
        effectRenderer.enabled = false;
        effectRenderer.shadowCastingMode = ShadowCastingMode.Off;
        effectRenderer.receiveShadows = false;
        effectRenderer.lightProbeUsage = LightProbeUsage.Off;
        effectRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        effectRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        material = new Material(shader) { name = "Emissão da travessia orbital" };
        effectRenderer.sharedMaterial = material;
        engineSource = effect.AddComponent<AudioSource>();
        engineSource.playOnAwake = false;
        engineSource.loop = true;
        engineSource.spatialBlend = 0f;
        engineSource.volume = 0f;
        engineLoop = CreateEngineLoop();
        engineSource.clip = engineLoop;

        var uv = new Vector2[positions.Length];
        for (int i = 0; i < MaxRibbons; i++)
        {
            int vertex = i * 4;
            uv[vertex] = Vector2.zero;
            uv[vertex + 1] = Vector2.right;
            uv[vertex + 2] = Vector2.one;
            uv[vertex + 3] = Vector2.up;
            int triangle = i * 6;
            indices[triangle] = indices[triangle + 3] = vertex;
            indices[triangle + 1] = vertex + 1;
            indices[triangle + 2] = indices[triangle + 4] = vertex + 2;
            indices[triangle + 5] = vertex + 3;
        }

        for (int i = 0; i < MoteCount; i++)
        {
            float angle = (i + Sample(i, 1) * 0.82f) * (2f * Mathf.PI / MoteCount);
            Vector2 radial = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            moteDirections[i] = radial * (0.49f / Mathf.Max(Mathf.Abs(radial.x), Mathf.Abs(radial.y)));
        }

        mesh = new Mesh { name = "Geometria de trânsito orbital" };
        mesh.MarkDynamic();
        mesh.SetVertices(positions);
        mesh.SetUVs(0, uv);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 840f);
        effect.GetComponent<MeshFilter>().sharedMesh = mesh;
        return true;
    }

    private static AudioClip CreateEngineLoop()
    {
        const int sampleRate = 22050;
        var samples = new float[sampleRate * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            float time = i * (2f * Mathf.PI / sampleRate);
            float cycle = 0.82f + Mathf.Sin(1.5f * time) * 0.11f + Mathf.Sin(3.5f * time) * 0.07f;
            float motor = Mathf.Sin(58f * time + Mathf.Sin(2f * time) * 0.15f) * 0.38f
                + Mathf.Sin(116f * time) * 0.17f + Mathf.Sin(174f * time) * 0.065f;
            float flow = (Mathf.Sin(213f * time) + Mathf.Sin(259f * time) + Mathf.Sin(317f * time)) * 0.016f;
            samples[i] = motor * cycle + flow;
        }

        var clip = AudioClip.Create("Motor da 14-Bis II em cruzeiro", samples.Length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static Color WithAlpha(Color value, float alpha)
    {
        value.a = alpha;
        return value;
    }

    private static float Sample(int element, int channel)
    {
        return Mathf.Repeat(Mathf.Sin(element * 17.319f + channel * 12.779f) * 43758.54f, 1f);
    }

    private void OnDestroy()
    {
        if (effectRenderer != null) Destroy(effectRenderer.gameObject);
        if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
        if (engineLoop != null) Destroy(engineLoop);
    }
}
