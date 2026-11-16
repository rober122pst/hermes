using UnityEngine;
using UnityEngine.Rendering;
using Player;

public class CoreTerminal : MonoBehaviour, IInteractive
{
    internal const float DockingDistance = 45f;
    private static readonly Color LinkColor = new Color(0.39f, 0.94f, 0.78f);

    private EnemySystem boss;
    private MarkCampaignHud hud;
    private Transform pilotView;
    private SphereCollider dockingSignal;
    private LineRenderer marker;
    private Material markerMaterial;
    private HermesPatch patch;
    private HermesPatchConsole console;
    private bool focused;

    internal static CoreTerminal Attach(EnemySystem boss, MarkCampaignHud hud, Shader signalShader)
    {
        var terminalObject = new GameObject("Terminal de ancoragem");
        terminalObject.transform.SetParent(boss.transform, false);
        terminalObject.transform.localPosition = new Vector3(0f, 1.1f, 10f);
        int interactLayer = LayerMask.NameToLayer("Interact");
        if (interactLayer >= 0) terminalObject.layer = interactLayer;
        var terminal = terminalObject.AddComponent<CoreTerminal>();
        terminal.boss = boss;
        terminal.hud = hud;
        terminal.pilotView = Camera.main != null ? Camera.main.transform : null;
        terminal.dockingSignal = terminalObject.AddComponent<SphereCollider>();
        terminal.dockingSignal.radius = 6f;
        terminal.dockingSignal.isTrigger = true;
        terminal.dockingSignal.enabled = false;
        terminal.CreateMarker(signalShader);
        return terminal;
    }

    public bool CanFocus(Transform ship)
    {
        return boss != null && boss.IsTerminalVulnerable && ship != null &&
            (ship.position - transform.position).sqrMagnitude <= DockingDistance * DockingDistance;
    }

    public void ShowDialogueBox()
    {
        focused = true;
    }

    public void HideDialogueBox()
    {
        focused = false;
    }

    public void OpenPanel(StarterAssetsInputs input)
    {
        if (console != null || !CanFocus(input != null ? input.transform : null) || hud == null ||
            UIManager.Instance == null || !UIManager.Instance.TryBeginTacticalPause(this, input)) return;

        patch ??= HermesPatches.RewriteEvacuation(ApplyPatch);
        console = HermesPatchConsole.Open(hud.CanvasRect, patch, "Desacoplar", PanelClosed);
    }

    private void ApplyPatch()
    {
        if (boss != null) boss.OverloadCore();
    }

    private void PanelClosed(bool applying)
    {
        console = null;
        UIManager.Instance?.EndTacticalPause(this);
    }

    private void LateUpdate()
    {
        bool exposed = boss != null && boss.IsTerminalVulnerable;
        dockingSignal.enabled = exposed;
        if (marker != null)
        {
            marker.enabled = exposed;
            if (exposed)
            {
                marker.widthMultiplier = focused ? 0.55f : 0.30f;
                Transform markerTransform = marker.transform;
                if (pilotView != null) markerTransform.rotation = pilotView.rotation;
                markerTransform.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(Time.time * 6f));
            }
        }
    }

    private void CreateMarker(Shader shader)
    {
        if (shader == null) return;
        markerMaterial = new Material(shader);
        markerMaterial.SetColor("_BaseColor", LinkColor);
        var markerObject = new GameObject("Baliza de ancoragem");
        markerObject.transform.SetParent(transform, false);
        marker = markerObject.AddComponent<LineRenderer>();
        marker.sharedMaterial = markerMaterial;
        marker.useWorldSpace = false;
        marker.loop = true;
        marker.shadowCastingMode = ShadowCastingMode.Off;
        marker.receiveShadows = false;
        marker.positionCount = 6;
        for (int i = 0; i < marker.positionCount; i++)
        {
            float angle = (i + 0.5f) * 2f * Mathf.PI / marker.positionCount;
            marker.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 7.2f);
        }
        marker.enabled = false;
    }

    private void OnDisable()
    {
        if (console != null) console.Close();
    }

    private void OnDestroy()
    {
        if (markerMaterial != null) Destroy(markerMaterial);
    }
}
