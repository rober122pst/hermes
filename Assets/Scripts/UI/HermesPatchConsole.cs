#nullable enable
using System;
using System.Globalization;
using HERMESLANG.Parser;
using HERMESLANG.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

internal sealed class HermesPatchConsole : MonoBehaviour
{
    private static readonly Color Ink = new Color(0.024f, 0.045f, 0.069f);
    private static readonly Color Well = new Color(0.014f, 0.031f, 0.049f);
    private static readonly Color Accent = new Color(0.44f, 0.96f, 0.82f);
    private static readonly Color Muted = new Color(0.59f, 0.74f, 0.79f);
    private static readonly Color Text = new Color(0.91f, 0.96f, 1f);
    private static readonly Color NeedsRepair = new Color(1f, 0.73f, 0.45f);

    private HermesPatch patch = null!;
    private Action<bool>? onClose;
    private Action? apply;
    private TextMeshProUGUI code = null!;
    private TextMeshProUGUI trace = null!;
    private TextMeshProUGUI testStatus = null!;
    private Button applyButton = null!;
    private Button[][] choices = null!;
    private int[] selected = null!;

    public static HermesPatchConsole Open(RectTransform canvas, HermesPatch patch, string exitLabel, Action<bool> onClose)
    {
        var overlay = Image("Editor HERMES", canvas, new Color(0.009f, 0.018f, 0.033f, 0.83f), true);
        Stretch(overlay.rectTransform);
        var console = overlay.gameObject.AddComponent<HermesPatchConsole>();
        console.Initialize(canvas, patch, exitLabel, onClose);
        return console;
    }

    private void Initialize(RectTransform canvas, HermesPatch program, string exitLabel, Action<bool> closed)
    {
        patch = program;
        onClose = closed;
        choices = new Button[patch.Fragments.Length][];
        selected = (int[])patch.InitialSelections.Clone();

        RectTransform shell = Image("Painel de depuração", transform, Ink).rectTransform;
        var center = new Vector2(0.5f, 0.5f);
        MarkCampaignHud.Place(shell, center, center, Vector2.zero, new Vector2(1136f, 660f));
        float scale = Mathf.Min(1f, (canvas.rect.width - 36f) / 1136f, (canvas.rect.height - 36f) / 660f);
        if (scale > 0f) shell.localScale = Vector3.one * scale;
        Line(shell, 0f, 0f, 1136f, 4f, Accent);

        Label("Terminal", shell, "TERMINAL HERMES", 14f, Accent, 33f, 22f, 1068f, 20f, FontStyles.Bold)
            .characterSpacing = 1.2f;
        Label("Rotina", shell, patch.Title, 32f, Text, 33f, 44f, 1068f, 42f, FontStyles.Bold);
        Label("Diagnóstico", shell, patch.Brief, 19f, Text, 35f, 94f, 1062f, 68f);
        Line(shell, 33f, 155f, 1070f, 1f, new Color(0.14f, 0.26f, 0.31f));

        RectTransform editor = Area(shell, "Rotina editável", 33f, 176f, 625f, 404f);
        RectTransform telemetry = Area(shell, "Bancada", 675f, 176f, 428f, 404f);

        Label("Código", editor, "ROTINA", 14f, Accent, 18f, 13f, 588f, 24f, FontStyles.Bold).characterSpacing = 1.2f;
        code = Label("Código HERMES", editor, string.Empty, 19f, Text, 19f, 44f, 590f, 245f);
        code.richText = true;
        code.textWrappingMode = TextWrappingModes.NoWrap;
        code.enableAutoSizing = true;
        code.fontSizeMin = 15f;
        code.fontSizeMax = 19f;
        code.lineSpacing = 5f;
        code.characterSpacing = 0.15f;

        Label("Telemetria", telemetry, "BANCADA DE TELEMETRIA", 14f, Accent, 18f, 13f, 392f, 24f, FontStyles.Bold)
            .characterSpacing = 1.1f;
        testStatus = Label("Estado da rotina", telemetry, "PATCH PENDENTE", 18f, NeedsRepair, 18f, 44f, 392f, 26f, FontStyles.Bold);
        trace = Label("Saída HERMES", telemetry, "Execute a rotina para diagnosticar o sistema.\nA bancada registra os efeitos de cada entrada.", 19f, Text,
            18f, 85f, 391f, 299f);
        trace.richText = false;

        for (int slot = 0; slot < choices.Length; slot++)
        {
            int row = slot;
            float top = 293f + slot * 48f;
            Label($"Fragmento {slot + 1}", editor, $"{slot + 1:00}", 16f, Accent, 20f, top + 8f, 35f, 26f, FontStyles.Bold);
            choices[slot] = new Button[patch.Fragments[slot].Length];
            for (int choice = 0; choice < choices[slot].Length; choice++)
            {
                int option = choice;
                Button fragment = ButtonAt(editor, patch.Fragments[slot][choice], 62f + choice * 165f, top,
                    153f, 39f, Muted, () => SelectFragment(row, option));
                var fragmentLabel = fragment.GetComponentInChildren<TextMeshProUGUI>();
                fragmentLabel.enableAutoSizing = true;
                fragmentLabel.fontSizeMin = 12f;
                fragmentLabel.fontSizeMax = 20f;
                fragmentLabel.textWrappingMode = TextWrappingModes.NoWrap;
                fragmentLabel.margin = new Vector4(6f, 0f, 6f, 0f);
                choices[slot][choice] = fragment;
            }
        }

        ButtonAt(shell, exitLabel, 33f, 602f, 175f, 41f, Muted, () => Close());
        ButtonAt(shell, "Testar patch", 742f, 602f, 168f, 41f, Muted, Run);
        applyButton = ButtonAt(shell, "Aplicar patch", 924f, 602f, 179f, 41f, Accent, Apply);
        applyButton.interactable = false;
        RefreshSource();

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(choices[0][selected[0]].gameObject);
    }

    private void Update()
    {
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true ||
            Gamepad.current?.buttonEast.wasPressedThisFrame == true)
            Close();
    }

    private void SelectFragment(int slot, int option)
    {
        if (selected[slot] == option) return;

        selected[slot] = option;
        apply = null;
        applyButton.interactable = false;
        testStatus.text = "PATCH ALTERADO";
        testStatus.color = NeedsRepair;
        trace.text = "A rotina mudou. Execute a bancada para validar o patch.";
        RefreshSource();
    }

    private void RefreshSource()
    {
        var replacements = new object[choices.Length];
        for (int slot = 0; slot < choices.Length; slot++)
        {
            for (int option = 0; option < choices[slot].Length; option++)
            {
                choices[slot][option].image.color = selected[slot] == option ? Accent : Muted;
            }
            string syntax = patch.Fragments[slot][selected[slot]];
            replacements[slot] = $"<color=#70F5D1><noparse>{syntax}</noparse></color><sup><color=#70F5D1>{slot + 1:00}</color></sup>";
        }
        code.text = string.Format(CultureInfo.InvariantCulture, patch.Source, replacements);
    }

    private void Run()
    {
        var replacements = new object[choices.Length];
        for (int slot = 0; slot < replacements.Length; slot++)
        {
            replacements[slot] = patch.Fragments[slot][selected[slot]];
        }
        string source = string.Format(CultureInfo.InvariantCulture, patch.Source, replacements);
        try
        {
            var tokens = new HERMESLANG.Lexer.Lexer(source).ScanTokens();
            var program = new Parser(tokens).Parse();
            HermesDiagnostic diagnostics = patch.Verify(program);
            trace.text = diagnostics.Trace;
            apply = diagnostics.Apply;
            testStatus.text = apply != null ? "PATCH VALIDADO" : "FALHA DE LÓGICA";
            testStatus.color = apply != null ? Accent : NeedsRepair;
        }
        catch (ParserException error)
        {
            MarkError("ERRO DE SINTAXE", error.Message);
        }
        catch (HermesException error)
        {
            MarkError("FALHA DE EXECUÇÃO", error.Message);
        }
        applyButton.interactable = apply != null;
        if (apply != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(applyButton.gameObject);
    }

    private void MarkError(string label, string message)
    {
        apply = null;
        testStatus.text = label;
        testStatus.color = NeedsRepair;
        trace.text = message;
    }

    private void Apply()
    {
        Action? commit = apply;
        if (commit == null) return;

        Close(true);
        commit();
    }

    public void Close(bool applying = false)
    {
        Action<bool>? callback = onClose;
        if (callback == null) return;

        onClose = null;
        apply = null;
        gameObject.SetActive(false);
        Destroy(gameObject);
        callback(applying);
    }

    private void OnDisable()
    {
        Action<bool>? callback = onClose;
        onClose = null;
        apply = null;
        callback?.Invoke(false);
    }

    private static RectTransform Area(RectTransform parent, string name, float left, float top, float width, float height)
    {
        var background = Image(name, parent, Well).rectTransform;
        MarkCampaignHud.Place(background, Vector2.up, Vector2.up, new Vector2(left, -top), new Vector2(width, height));
        return background;
    }

    private static void Line(RectTransform parent, float left, float top, float width, float height, Color color)
    {
        var line = Image("Divisor", parent, color).rectTransform;
        MarkCampaignHud.Place(line, Vector2.up, Vector2.up, new Vector2(left, -top), new Vector2(width, height));
    }

    private static TextMeshProUGUI Label(string name, RectTransform parent, string value, float size, Color color,
        float left, float top, float width, float height, FontStyles style = FontStyles.Normal)
    {
        var label = MarkCampaignHud.Label(name, parent, size, color, TextAlignmentOptions.TopLeft, style);
        MarkCampaignHud.Place(label.rectTransform, Vector2.up, Vector2.up, new Vector2(left, -top), new Vector2(width, height));
        label.text = value;
        label.richText = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        return label;
    }

    private static Button ButtonAt(RectTransform parent, string title, float left, float top, float width,
        float height, Color color, Action click)
    {
        var button = MarkCampaignHud.CreateButton(title, parent, Vector2.zero, new Vector2(width, height), color, click);
        RectTransform rect = (RectTransform)button.transform;
        MarkCampaignHud.Place(rect, Vector2.up, Vector2.up, new Vector2(left, -top), new Vector2(width, height));
        button.GetComponentInChildren<TextMeshProUGUI>().richText = false;
        return button;
    }

    private static Image Image(string name, Transform parent, Color color, bool interactive = false)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        image.raycastTarget = interactive;
        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
