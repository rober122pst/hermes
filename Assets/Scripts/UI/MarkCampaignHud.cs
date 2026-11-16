using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed class MarkCampaignHud
{
    private static readonly Color Ink = new Color(0.035f, 0.06f, 0.10f, 0.96f);
    private static readonly Color White = new Color(0.91f, 0.96f, 1f);
    private static readonly Color Muted = new Color(0.68f, 0.79f, 0.86f);
    private static readonly Color HullHealthy = new Color(0.36f, 0.89f, 0.84f);
    private static readonly Color HullCritical = new Color(1f, 0.42f, 0.49f);

    private readonly RectTransform root;
    private readonly Image signal;
    private readonly TextMeshProUGUI sectorLabel;
    private readonly TextMeshProUGUI waveLabel;
    private readonly TextMeshProUGUI statusLabel;
    private readonly Image hullFill;
    private readonly Image hullChip;
    private readonly TextMeshProUGUI hullLoss;
    private readonly HullImpactGraphic hullImpact;
    private readonly TextMeshProUGUI notice;
    private readonly CanvasGroup radio;
    private readonly Image radioAccent;
    private readonly TextMeshProUGUI radioSpeaker;
    private readonly TextMeshProUGUI radioLine;
    private readonly RectTransform bossPanel;
    private readonly TextMeshProUGUI bossPrompt;
    private readonly RectTransform bossFill;
    private readonly Image bossFillColor;
    private readonly TextMeshProUGUI bossDirection;
    private readonly RectTransform navigationPanel;
    private readonly TextMeshProUGUI navigationTitle;
    private readonly TextMeshProUGUI navigationReadout;
    private readonly TextMeshProUGUI navigationHint;
    private readonly RectTransform waypoint;
    private readonly RectTransform waypointFrame;
    private readonly RectTransform waypointArrow;
    private readonly Image[] waypointLines;
    private readonly Image navigationRule;
    private float noticeExpiry;
    private float radioStart;
    private float radioExpiry;
    private float hitTime = float.NegativeInfinity;
    private Vector2 hitBearing;
    private float hitStrength;
    private float hullFraction = 1f;
    private float chipFraction = 1f;
    private int lastHealth = -1;
    private int lastGems = -1;
    private int lastRemaining = -1;
    private Color signalColor;
    private bool? bossLinkAvailable;
    private bool? pilotCanLink;

    internal RectTransform CanvasRect => (RectTransform)root.parent;

    public MarkCampaignHud(Canvas canvas)
    {
        root = Rect("Campanha de Mark", canvas.transform);
        Stretch(root);

        RectTransform impactLayer = Rect("Impacto no casco", root);
        Stretch(impactLayer);
        hullImpact = impactLayer.gameObject.AddComponent<HullImpactGraphic>();
        hullImpact.raycastTarget = false;
        hullImpact.SetImpact(Vector2.zero, 0f);

        RectTransform status = Panel("Objetivos", root, Ink);
        Place(status, Vector2.up, Vector2.up, new Vector2(24f, -53f), new Vector2(326f, 142f));

        signal = Bar(status, Vector2.up, Vector2.up, Vector2.zero, new Vector2(4f, 142f), Color.cyan);
        sectorLabel = Label("Setor", status, 24f, White, TextAlignmentOptions.Left, FontStyles.Bold);
        Place(sectorLabel.rectTransform, Vector2.up, Vector2.up, new Vector2(18f, -11f), new Vector2(288f, 29f));
        waveLabel = Label("Onda", status, 16f, Muted);
        Place(waveLabel.rectTransform, Vector2.up, Vector2.up, new Vector2(18f, -45f), new Vector2(288f, 24f));
        statusLabel = Label("Nave", status, 16f, White);
        Place(statusLabel.rectTransform, Vector2.up, Vector2.up, new Vector2(18f, -80f), new Vector2(288f, 22f));
        Image hullTrack = Bar(status, Vector2.up, Vector2.up, new Vector2(18f, -109f),
            new Vector2(288f, 7f), new Color(0.18f, 0.24f, 0.30f));
        hullChip = Image("Dano recente", hullTrack.transform, HullCritical);
        hullFill = Image("Integridade do casco", hullTrack.transform, HullHealthy);
        Stretch(hullChip.rectTransform);
        Stretch(hullFill.rectTransform);
        hullLoss = Label("Perda no casco", status, 22f, HullCritical, TextAlignmentOptions.Right, FontStyles.Bold);
        Place(hullLoss.rectTransform, Vector2.up, Vector2.up, new Vector2(216f, -43f), new Vector2(90f, 28f));
        hullLoss.gameObject.SetActive(false);

        notice = Label("Evento do setor", root, 24f, White, TextAlignmentOptions.Center);
        notice.textWrappingMode = TextWrappingModes.Normal;
        Place(notice.rectTransform, new Vector2(0.5f, 0.81f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(780f, 110f));
        var shadow = notice.gameObject.AddComponent<Shadow>();
        shadow.effectDistance = new Vector2(2f, -2f);
        shadow.effectColor = Color.black;

        var radioAnchor = new Vector2(0.5f, 0.17f);
        var radioPanel = Panel("Rádio de bordo", root, new Color(0.035f, 0.06f, 0.10f, 0.91f));
        Place(radioPanel, radioAnchor, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(694f, 106f));
        radio = radioPanel.gameObject.AddComponent<CanvasGroup>();
        radio.interactable = false;
        radio.blocksRaycasts = false;
        radioAccent = Bar(radioPanel, Vector2.up, Vector2.up, Vector2.zero, new Vector2(4f, 106f), Color.cyan);
        radioSpeaker = Label("Origem da transmissão", radioPanel, 17f, White, TextAlignmentOptions.Left, FontStyles.Bold);
        radioSpeaker.characterSpacing = 2f;
        Place(radioSpeaker.rectTransform, Vector2.up, Vector2.up, new Vector2(20f, -8f), new Vector2(650f, 23f));
        radioLine = Label("Transmissão", radioPanel, 21f, White);
        radioLine.textWrappingMode = TextWrappingModes.Normal;
        Place(radioLine.rectTransform, Vector2.up, Vector2.up, new Vector2(20f, -33f), new Vector2(650f, 62f));
        radio.gameObject.SetActive(false);

        bossPanel = Panel("Núcleo", root, Ink);
        Place(bossPanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -53f), new Vector2(550f, 98f));
        var bossLabel = Label("Identificação", bossPanel, 21f, White, TextAlignmentOptions.Center, FontStyles.Bold);
        bossLabel.text = "ROBÔ GIGANTE  ·  NÚCLEO H.E.R.M.E.S.";
        Place(bossLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -9f), new Vector2(520f, 29f));
        var track = Bar(bossPanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -42f), new Vector2(488f, 8f), new Color(0.20f, 0.24f, 0.31f));
        bossFillColor = Image("Integridade", track.rectTransform, Color.magenta);
        bossFill = bossFillColor.rectTransform;
        Stretch(bossFill);
        bossPrompt = Label("Instrução", bossPanel, 17f, White, TextAlignmentOptions.Center);
        Place(bossPrompt.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -59f), new Vector2(520f, 28f));
        bossDirection = Label("Direção do núcleo", root, 16f, White, TextAlignmentOptions.Center, FontStyles.Bold);
        bossDirection.text = "NÚCLEO";
        bossDirection.gameObject.SetActive(false);
        bossPanel.gameObject.SetActive(false);

        navigationPanel = Panel("Navegação", root, Ink);
        var top = new Vector2(0.5f, 1f);
        Place(navigationPanel, top, top, new Vector2(0f, -53f), new Vector2(458f, 107f));
        navigationRule = Bar(navigationPanel, top, top, Vector2.zero, new Vector2(458f, 3f), Color.cyan);
        navigationTitle = Label("Rota", navigationPanel, 24f, White, TextAlignmentOptions.Center, FontStyles.Bold);
        Place(navigationTitle.rectTransform, top, top, new Vector2(0f, -11f), new Vector2(436f, 28f));
        navigationReadout = Label("Distância e velocidade", navigationPanel, 17f, White, TextAlignmentOptions.Center);
        Place(navigationReadout.rectTransform, top, top, new Vector2(0f, -41f), new Vector2(436f, 25f));
        navigationHint = Label("Comando", navigationPanel, 17f, Muted, TextAlignmentOptions.Center);
        Place(navigationHint.rectTransform, top, top, new Vector2(0f, -70f), new Vector2(436f, 26f));

        waypoint = Rect("Baliza de navegação", root);
        waypointFrame = Rect("Marcador", waypoint);
        waypointFrame.localRotation = Quaternion.Euler(0f, 0f, 45f);
        waypointArrow = Rect("Seta", waypoint);
        Vector2 center = Vector2.one * 0.5f;
        waypointLines = new[]
        {
            Bar(waypointFrame, center, center, new Vector2(0f, 10f), new Vector2(22f, 2f), White),
            Bar(waypointFrame, center, center, new Vector2(0f, -10f), new Vector2(22f, 2f), White),
            Bar(waypointFrame, center, center, new Vector2(10f, 0f), new Vector2(2f, 22f), White),
            Bar(waypointFrame, center, center, new Vector2(-10f, 0f), new Vector2(2f, 22f), White),
            Bar(waypointArrow, center, center, new Vector2(0f, 6f), new Vector2(21f, 3f), White),
            Bar(waypointArrow, center, center, new Vector2(0f, -6f), new Vector2(21f, 3f), White)
        };
        waypointLines[4].rectTransform.localRotation = Quaternion.Euler(0f, 0f, -35f);
        waypointLines[5].rectTransform.localRotation = Quaternion.Euler(0f, 0f, 35f);
        foreach (Image line in waypointLines)
        {
            var outline = line.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0.035f, 0.07f, 0.95f);
        }
        EndNavigation();
    }

    public void EnterSector(int sector, int total, string title, string briefing, Color color)
    {
        signalColor = color;
        signal.color = color;
        sectorLabel.text = title;
        waveLabel.text = string.Empty;
        ShowNotice($"SETOR {sector}/{total}  ·  {title}\n{briefing}", 6.5f);
    }

    public void SetWave(int wave, int total)
    {
        waveLabel.SetText("Onda {0}/{1}", wave, total);
        lastRemaining = -1;
    }

    public void SetStatus(int health, int maxHealth, int gems, int remaining)
    {
        if (health == lastHealth && gems == lastGems && remaining == lastRemaining) return;
        lastHealth = health;
        lastGems = gems;
        lastRemaining = remaining;
        statusLabel.SetText("Casco {0}/{1}  ·  Joias {2}  ·  Alvos {3}", health, maxHealth, gems, remaining);
        hullFraction = maxHealth > 0 ? Mathf.Clamp01(health / (float)maxHealth) : 0f;
        hullFill.rectTransform.anchorMax = new Vector2(hullFraction, 1f);
        hullFill.color = hullFraction > 0.3f ? HullHealthy : HullCritical;
        if (hullFraction > chipFraction) chipFraction = hullFraction;
    }

    public void ShowHullImpact(float damage, Vector2 bearing)
    {
        hitTime = Time.unscaledTime;
        hitBearing = bearing.normalized;
        hitStrength = Mathf.Lerp(0.66f, 1f, Mathf.InverseLerp(5f, 25f, damage));
        hullLoss.SetText("−{0}", Mathf.CeilToInt(damage));
        hullLoss.alpha = 1f;
        hullLoss.gameObject.SetActive(true);
        hullImpact.SetImpact(hitBearing, hitStrength);
    }

    public void Transmit(string speaker, string line, Color accent, float seconds)
    {
        radioSpeaker.text = speaker;
        radioSpeaker.color = accent;
        radioAccent.color = accent;
        radioLine.text = line;
        radioStart = Time.time;
        radioExpiry = radioStart + seconds;
        radio.alpha = 0f;
        radio.gameObject.SetActive(true);
    }

    public void TickPresentation()
    {
        if (notice.gameObject.activeSelf && Time.time >= noticeExpiry) notice.gameObject.SetActive(false);
        if (radio.gameObject.activeSelf)
        {
            float remaining = radioExpiry - Time.time;
            radio.alpha = Mathf.Min(Mathf.Clamp01((Time.time - radioStart) / 0.15f), Mathf.Clamp01(remaining / 0.33f));
            if (remaining <= 0f) radio.gameObject.SetActive(false);
        }

        float age = Time.unscaledTime - hitTime;
        if (hullImpact.gameObject.activeSelf)
        {
            float flash = Mathf.Clamp01(1f - age / 0.42f);
            hullImpact.SetImpact(hitBearing, hitStrength * flash * flash);
        }
        if (hullLoss.gameObject.activeSelf)
        {
            hullLoss.alpha = Mathf.Clamp01((0.92f - age) / 0.36f);
            if (age >= 0.92f) hullLoss.gameObject.SetActive(false);
        }
        if (age >= 0.13f)
        {
            chipFraction = Mathf.MoveTowards(chipFraction, hullFraction, Time.unscaledDeltaTime * 0.85f);
        }
        hullChip.rectTransform.anchorMax = new Vector2(chipFraction, 1f);
    }

    public void ShowNotice(string message, float seconds)
    {
        notice.text = message;
        noticeExpiry = Time.time + seconds;
        notice.gameObject.SetActive(true);
    }

    public void SetBossStatus(float healthRatio, bool exposed, bool canLink)
    {
        if (!bossPanel.gameObject.activeSelf) bossPanel.gameObject.SetActive(true);
        bossFill.anchorMax = new Vector2(Mathf.Clamp01(healthRatio), 1f);
        if (bossLinkAvailable == exposed && pilotCanLink == canLink) return;

        bossLinkAvailable = exposed;
        pilotCanLink = canLink;
        bossFillColor.color = exposed ? new Color(0.40f, 0.94f, 0.79f) : signalColor;
        bossPrompt.text = canLink
            ? "Espaço / A para ancorar e abrir o terminal"
            : exposed ? "Janela aberta · Aproxime-se do núcleo verde"
            : "Abra uma janela no escudo com os canhões";
    }

    public void HideBoss()
    {
        bossPanel.gameObject.SetActive(false);
        bossDirection.gameObject.SetActive(false);
    }

    public void PointToBoss(Vector3 viewport)
    {
        var direction = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f);
        bool inFront = viewport.z > 0f;
        if (inFront && Mathf.Abs(direction.x) <= 0.44f && Mathf.Abs(direction.y) <= 0.37f)
        {
            bossDirection.gameObject.SetActive(false);
            return;
        }

        if (!inFront) direction = -direction;
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;
        float edge = Mathf.Max(Mathf.Abs(direction.x) / 0.44f, Mathf.Abs(direction.y) / 0.35f);
        Vector2 anchor = Vector2.one * 0.5f + (direction / edge);
        Place(bossDirection.rectTransform, anchor, Vector2.one * 0.5f, Vector2.zero, new Vector2(113f, 32f));
        bossDirection.color = bossFillColor.color;
        bossDirection.gameObject.SetActive(true);
    }

    public void BeginNavigation(string title, Color color)
    {
        navigationTitle.text = title;
        navigationRule.color = color;
        foreach (Image line in waypointLines) line.color = color;
        navigationPanel.gameObject.SetActive(true);
    }

    public void UpdateNavigation(string hint, float distance, float speed)
    {
        if (navigationHint.text != hint) navigationHint.text = hint;
        navigationReadout.SetText("{0} m até a baliza  ·  {1} m/s", Mathf.CeilToInt(distance), Mathf.RoundToInt(speed));
    }

    public void PointToNavigation(Camera camera, Vector3 position)
    {
        Vector3 local = camera.transform.InverseTransformPoint(position);
        Vector2 center = Vector2.one * 0.5f;
        Vector2 anchor;
        bool inFront = local.z > camera.nearClipPlane;
        Vector3 viewport = inFront ? camera.WorldToViewportPoint(position) : Vector3.zero;
        bool onScreen = inFront && viewport.x >= 0.06f && viewport.x <= 0.94f &&
            viewport.y >= 0.12f && viewport.y <= 0.82f;
        if (onScreen)
        {
            anchor = viewport;
        }
        else
        {
            Vector2 direction = new Vector2(local.x / camera.aspect, local.y);
            if (direction.sqrMagnitude < 0.01f) direction = inFront ? Vector2.up : Vector2.right;
            float scale = Mathf.Max(Mathf.Abs(direction.x) / 0.44f, Mathf.Abs(direction.y) / 0.32f);
            anchor = center + direction / scale;
            float angle = Mathf.Atan2(direction.y, direction.x * camera.aspect) * Mathf.Rad2Deg;
            waypointArrow.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        Place(waypoint, anchor, center, Vector2.zero, new Vector2(32f, 32f));
        waypointFrame.gameObject.SetActive(onScreen);
        waypointArrow.gameObject.SetActive(!onScreen);
        waypoint.gameObject.SetActive(true);
    }

    public void EndNavigation()
    {
        navigationPanel.gameObject.SetActive(false);
        waypoint.gameObject.SetActive(false);
    }

    public void ShowOutcome(bool victory, int gems, Action restart)
    {
        notice.gameObject.SetActive(false);
        radio.gameObject.SetActive(false);
        Color color = victory ? new Color(0.4f, 0.94f, 0.79f) : new Color(1f, 0.59f, 0.53f);
        var panel = Overlay("Resultado", new Vector2(660f, 320f), color);
        var title = Label("Estado", panel, 42f, White, TextAlignmentOptions.Center, FontStyles.Bold);
        title.text = victory ? "ROTA REPROGRAMADA" : "14-BIS II ABATIDA";
        Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -43f), new Vector2(600f, 58f));

        var description = Label("Rádio", panel, 22f, Muted, TextAlignmentOptions.Center);
        description.textWrappingMode = TextWrappingModes.Normal;
        string report = victory
            ? "Lara: A rota está aberta. Desta vez, as famílias terão a mesma chance."
            : "Lara: Cápsula ejetada. Dados das Joias Rosas transmitidos à base.";
        description.text = $"{report}\nJoias recuperadas nesta missão: {gems}";
        Place(description.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -119f), new Vector2(575f, 87f));

        var restartButton = CreateButton("Reiniciar missão", panel, new Vector2(0f, -238f), new Vector2(270f, 49f), color, restart);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
    }

    public void Dispose()
    {
        if (root != null) UnityEngine.Object.Destroy(root.gameObject);
    }

    private RectTransform Overlay(string name, Vector2 size, Color accent)
    {
        var backdrop = Image(name, root, new Color(0.015f, 0.022f, 0.036f, 0.82f), true);
        Stretch(backdrop.rectTransform);
        var window = Panel("Janela", backdrop.rectTransform, Ink);
        Place(window, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, size);
        Bar(window, Vector2.up, Vector2.up, Vector2.zero, new Vector2(size.x, 5f), accent);
        return window;
    }

    internal static TextMeshProUGUI Label(string name, Transform parent, float fontSize, Color color,
        TextAlignmentOptions alignment = TextAlignmentOptions.Left, FontStyles style = FontStyles.Normal)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        var text = obj.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = style;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    internal static Button CreateButton(string caption, Transform parent, Vector2 offset, Vector2 size, Color color, Action click)
    {
        var image = Image(caption, parent, color, true);
        Place(image.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), offset, size);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => click());
        var label = Label("Texto", image.transform, 20f, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        label.text = caption;
        Stretch(label.rectTransform);
        return button;
    }

    internal static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 offset, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = offset;
        rect.sizeDelta = size;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return (RectTransform)obj.transform;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static RectTransform Panel(string name, Transform parent, Color color)
    {
        return Image(name, parent, color).rectTransform;
    }

    private static Image Bar(Transform parent, Vector2 anchor, Vector2 pivot, Vector2 offset, Vector2 size, Color color)
    {
        var bar = Image("Linha", parent, color);
        Place(bar.rectTransform, anchor, pivot, offset, size);
        return bar;
    }

    private static Image Image(string name, Transform parent, Color color, bool raycastTarget = false)
    {
        var image = Rect(name, parent).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }
}
