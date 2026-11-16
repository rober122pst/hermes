using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

internal sealed class MarkSectorScenery : MonoBehaviour
{
    public enum Layout
    {
        Orbit,
        Debris,
        Storm
    }

    private GameObject sectorRoot;
    private Mesh asteroidMesh;
    private Material basalt;
    private Material iron;
    private Material plasma;
    private const float HideDistance = 480f;
    private const float RevealDistance = 420f;
    private readonly List<Transform> solarBands = new List<Transform>();

    public void Enter(Layout layout, Vector3 origin, Quaternion heading, Color signalColor, Shader signalShader)
    {
        Clear();
        if (layout == Layout.Orbit) return;

        sectorRoot = new GameObject("Paisagem de Mark");
        sectorRoot.transform.SetPositionAndRotation(origin, heading);
        sectorRoot.transform.SetParent(transform, true);

        if (layout == Layout.Debris)
            BuildDebris(signalColor);
        else
            BuildStorm(signalColor, signalShader);
    }

    public void UpdateVisibility(Vector3 observer)
    {
        if (sectorRoot == null) return;

        float distanceSquared = (observer - sectorRoot.transform.position).sqrMagnitude;
        bool visible = sectorRoot.activeSelf
            ? distanceSquared < HideDistance * HideDistance
            : distanceSquared <= RevealDistance * RevealDistance;
        if (sectorRoot.activeSelf != visible) sectorRoot.SetActive(visible);
    }

    public void Clear()
    {
        if (sectorRoot != null)
        {
            sectorRoot.SetActive(false);
            Destroy(sectorRoot);
        }
        sectorRoot = null;
        solarBands.Clear();
        if (asteroidMesh != null) Destroy(asteroidMesh);
        if (basalt != null) Destroy(basalt);
        if (iron != null) Destroy(iron);
        if (plasma != null) Destroy(plasma);
        asteroidMesh = null;
        basalt = null;
        iron = null;
        plasma = null;
    }

    private void BuildDebris(Color signalColor)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) return;

        asteroidMesh = BuildAsteroidMesh();
        basalt = new Material(shader);
        basalt.SetColor("_BaseColor", new Color(0.28f, 0.15f, 0.115f));
        basalt.SetFloat("_Smoothness", 0.06f);
        iron = new Material(shader);
        iron.SetColor("_BaseColor", Color.Lerp(new Color(0.53f, 0.27f, 0.19f), signalColor, 0.09f));
        iron.SetFloat("_Smoothness", 0.19f);

        var random = new System.Random(1402);
        for (int i = 0; i < 24; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            float depth = 42f + i / 2 * 15f;
            float size = Range(random, 4f, 13f);
            var asteroid = new GameObject("Fragmento de ferro");
            asteroid.transform.SetParent(sectorRoot.transform, false);
            asteroid.transform.localPosition = new Vector3(side * Range(random, 27f, 78f), Range(random, -26f, 24f), depth);
            asteroid.transform.localRotation = Quaternion.Euler(Range(random, 0f, 180f), Range(random, 0f, 180f), Range(random, 0f, 180f));
            asteroid.transform.localScale = new Vector3(size, size * Range(random, 0.64f, 1f), size * Range(random, 0.8f, 1.25f));
            asteroid.AddComponent<MeshFilter>().sharedMesh = asteroidMesh;
            asteroid.AddComponent<MeshRenderer>().sharedMaterial = i % 3 == 0 ? iron : basalt;

            // Bits farther from the flight corridor are environmental, so no collision checks are needed.
            if (i >= 12 || size < 6f) continue;
            asteroid.AddComponent<SphereCollider>().radius = 0.67f;
            var body = asteroid.AddComponent<Rigidbody>();
            body.mass = Mathf.Lerp(250f, 500f, size / 13f);
            body.useGravity = false;
            body.linearDamping = 0.55f;
            body.angularDamping = 0.6f;
        }
    }

    private static Mesh BuildAsteroidMesh()
    {
        Vector3 top = new Vector3(-0.16f, 0.79f, 0.18f);
        Vector3 bottom = new Vector3(0.12f, -0.66f, -0.27f);
        Vector3[] middle =
        {
            new Vector3(0.84f, 0.12f, 0.23f),
            new Vector3(0.36f, -0.04f, 0.92f),
            new Vector3(-0.43f, 0.19f, 0.71f),
            new Vector3(-0.79f, -0.06f, 0.12f),
            new Vector3(-0.21f, 0.08f, -0.68f),
            new Vector3(0.62f, -0.17f, -0.51f)
        };
        var vertices = new Vector3[6 * middle.Length];
        var triangles = new int[vertices.Length];
        for (int i = 0; i < middle.Length; i++)
        {
            int first = i * 6;
            Vector3 next = middle[(i + 1) % middle.Length];
            vertices[first] = top;
            vertices[first + 1] = next;
            vertices[first + 2] = middle[i];
            vertices[first + 3] = bottom;
            vertices[first + 4] = middle[i];
            vertices[first + 5] = next;
        }
        for (int i = 0; i < triangles.Length; i++) triangles[i] = i;

        var mesh = new Mesh { name = "Fragmentos facetados de Mark", vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals();
        return mesh;
    }

    private void BuildStorm(Color signalColor, Shader shader)
    {
        if (shader == null) return;

        plasma = new Material(shader);
        plasma.SetColor("_BaseColor", Color.Lerp(signalColor, new Color(1f, 0.33f, 0.10f), 0.5f));

        for (int i = 0; i < 9; i++)
        {
            var band = new GameObject("Faixa de plasma solar");
            band.transform.SetParent(sectorRoot.transform, false);
            band.transform.localPosition = new Vector3(8f * (i - 4), -56f + i * 14f, 205f + i * 17f);
            solarBands.Add(band.transform);

            var line = band.AddComponent<LineRenderer>();
            line.sharedMaterial = plasma;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.useWorldSpace = false;
            line.positionCount = 23;
            line.startWidth = 0.14f;
            line.endWidth = 0.85f;
            for (int j = 0; j < line.positionCount; j++)
            {
                float segment = j / (line.positionCount - 1f);
                line.SetPosition(j, new Vector3(-265f + 530f * segment,
                    Mathf.Sin(segment * 5.1f + i * 0.45f) * (12f + i * 1.6f), 0f));
            }
        }
    }

    private void Update()
    {
        if (sectorRoot == null || !sectorRoot.activeSelf) return;

        for (int i = 0; i < solarBands.Count; i++)
        {
            Transform band = solarBands[i];
            Vector3 position = band.localPosition;
            position.x = (i - 4) * 8f + Mathf.Sin(0.36f * Time.time + i) * 28f;
            position.y = -56f + i * 14f + Mathf.Sin(0.58f * Time.time + i * 0.49f) * 4f;
            band.localPosition = position;
        }
    }

    private static float Range(System.Random random, float min, float max)
    {
        return Mathf.Lerp(min, max, (float)random.NextDouble());
    }

    private void OnDestroy()
    {
        Clear();
    }
}
