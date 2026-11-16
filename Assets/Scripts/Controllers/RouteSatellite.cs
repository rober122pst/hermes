using System;
using Player;
using UnityEngine;
using UnityEngine.Rendering;

internal sealed class RouteSatellite : MonoBehaviour, IInteractive
{
    internal const float DockingDistance = 42f;

    private static readonly Color LinkColor = new(0.41f, 0.96f, 0.82f);

    private Canvas hudCanvas;
    private Camera pilotCamera;
    private Action<Vector3> onSolved;
    private Vector3 destinationCoordinates;
    private LineRenderer beacon;
    private Material antennaMaterial;
    private Material hullMaterial;
    private Material panelMaterial;
    private HermesPatch patch;
    private HermesPatchConsole console;
    private int destinationSector;
    private bool focused;

    public bool IsSolved { get; private set; }
    private bool IsSignalPuzzle => destinationSector == 2;

    internal static RouteSatellite Attach(Vector3 position, Transform parent, int destinationSector,
        Vector3 destinationCoordinates, Action<Vector3> onSolved, Shader signalShader, Canvas hudCanvas = null)
    {
        var relay = new GameObject("Satélite de navegação");
        relay.transform.SetParent(parent);
        relay.transform.SetPositionAndRotation(position, Quaternion.identity);
        int interactLayer = LayerMask.NameToLayer("Interact");
        if (interactLayer >= 0) relay.layer = interactLayer;

        var terminal = relay.AddComponent<RouteSatellite>();
        terminal.destinationSector = destinationSector;
        terminal.onSolved = onSolved;
        terminal.destinationCoordinates = destinationCoordinates;
        terminal.hudCanvas = hudCanvas;
        terminal.pilotCamera = Camera.main;

        var dockingSignal = relay.AddComponent<SphereCollider>();
        dockingSignal.radius = 7f;
        dockingSignal.isTrigger = true;
        terminal.CreateSatellite(signalShader);
        return terminal;
    }

    public bool CanFocus(Transform ship)
    {
        return isActiveAndEnabled && !IsSolved && ship != null &&
            (ship.position - transform.position).sqrMagnitude <= DockingDistance * DockingDistance;
    }

    public void ShowDialogueBox() => focused = true;

    public void HideDialogueBox() => focused = false;

    public void OpenPanel(StarterAssetsInputs input)
    {
        if (console != null || input == null || !CanFocus(input.transform)) return;

        UIManager ui = UIManager.Instance;
        if (ui == null) return;
        if (hudCanvas == null && ui.LevelUpScreen != null)
            hudCanvas = ui.LevelUpScreen.GetComponentInParent<Canvas>();
        if (hudCanvas == null || !ui.TryBeginTacticalPause(this, input)) return;

        patch ??= IsSignalPuzzle
            ? HermesPatches.RecoverPulse(destinationCoordinates, CompleteRoute)
            : HermesPatches.TriangulateRoute(transform.position, destinationCoordinates, CompleteRoute);
        console = HermesPatchConsole.Open((RectTransform)hudCanvas.transform, patch, "Desacoplar", PanelClosed);
    }

    private void CompleteRoute(Vector3 coordinates)
    {
        if (IsSolved) return;

        IsSolved = true;
        if (beacon != null) beacon.enabled = false;
        Action<Vector3> releaseRoute = onSolved;
        onSolved = null;
        releaseRoute?.Invoke(coordinates);
    }

    private void PanelClosed(bool applying)
    {
        console = null;
        UIManager.Instance?.EndTacticalPause(this);
    }

    private void LateUpdate()
    {
        if (beacon == null || IsSolved) return;

        if (pilotCamera != null) beacon.transform.rotation = pilotCamera.transform.rotation;
        beacon.widthMultiplier = focused ? 0.50f : 0.30f;
        beacon.transform.localScale = Vector3.one * (1f + 0.09f * Mathf.Sin(Time.time * 4f));
    }

    private void CreateSatellite(Shader signalShader)
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? signalShader;
        Shader unlit = signalShader ?? Shader.Find("Universal Render Pipeline/Unlit") ?? lit;
        if (lit == null || unlit == null) return;

        hullMaterial = new Material(lit) { color = new Color(0.53f, 0.61f, 0.68f) };
        panelMaterial = new Material(lit) { color = new Color(0.10f, 0.20f, 0.38f) };
        antennaMaterial = new Material(unlit) { color = LinkColor };

        var visual = new GameObject("Estrutura e painéis").transform;
        visual.SetParent(transform, false);
        visual.localRotation = Quaternion.Euler(15f, -24f, 10f);
        AddPart("Corpo", PrimitiveType.Cube, visual, Vector3.zero, new Vector3(5f, 4.2f, 4f), hullMaterial);
        AddPart("Estrutura dos painéis", PrimitiveType.Cube, visual, Vector3.zero, new Vector3(15.6f, 0.45f, 0.55f), hullMaterial);
        AddPart("Mastro", PrimitiveType.Cylinder, visual, new Vector3(0f, 2.6f, 0f), new Vector3(0.45f, 1.2f, 0.45f), hullMaterial);
        AddPart("Refletor", PrimitiveType.Sphere, visual, new Vector3(0f, 3.9f, 0f), new Vector3(5.5f, 1.2f, 5.5f), hullMaterial);
        AddPart("Emissor", PrimitiveType.Sphere, visual, new Vector3(0f, 5.0f, 0f), Vector3.one, antennaMaterial);

        for (int side = -1; side <= 1; side += 2)
        {
            AddPart("Painel solar", PrimitiveType.Cube, visual, new Vector3(side * 6.7f, 0f, 0f),
                new Vector3(5.7f, 0.28f, 3.5f), panelMaterial);
            AddPart("Luz de posição", PrimitiveType.Cube, visual, new Vector3(side * 9.4f, 0f, 0f),
                new Vector3(0.2f, 0.42f, 3.5f), antennaMaterial);
        }

        var signal = new GameObject("Baliza do satélite");
        signal.transform.SetParent(transform, false);
        beacon = signal.AddComponent<LineRenderer>();
        beacon.sharedMaterial = antennaMaterial;
        beacon.useWorldSpace = false;
        beacon.loop = true;
        beacon.shadowCastingMode = ShadowCastingMode.Off;
        beacon.receiveShadows = false;
        beacon.positionCount = 6;
        for (int i = 0; i < beacon.positionCount; i++)
        {
            float angle = Mathf.PI / 3f * i;
            beacon.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 11.5f);
        }
    }

    private static void AddPart(string name, PrimitiveType shape, Transform parent,
        Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(shape);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        Collider collider = part.GetComponent<Collider>();
        collider.enabled = false;
        Destroy(collider);
        part.GetComponent<Renderer>().sharedMaterial = material;
    }

    private void OnDisable()
    {
        if (console != null) console.Close();
    }

    private void OnDestroy()
    {
        if (antennaMaterial != null) Destroy(antennaMaterial);
        if (hullMaterial != null) Destroy(hullMaterial);
        if (panelMaterial != null) Destroy(panelMaterial);
    }
}
