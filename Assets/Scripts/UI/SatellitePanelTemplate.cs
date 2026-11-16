using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Monta um painel provisório no Canvas para a interação com o satélite.
public static class SatellitePanelTemplate
{
    public static GameObject Create(Canvas canvas, Action onClose)
    {
        GameObject overlay = new GameObject("SatellitePanelTemplate", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.SetParent(canvas.transform, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(0.015f, 0.025f, 0.045f, 0.78f);
        overlay.GetComponent<CanvasGroup>().alpha = 0f;

        GameObject window = CreateImage("Window", overlayRect, new Vector2(680f, 390f), Vector2.zero,
            new Color(0.055f, 0.09f, 0.15f, 1f));
        RectTransform windowRect = window.GetComponent<RectTransform>();
        windowRect.localScale = Vector3.one * 0.92f;

        CreateImage("Accent", windowRect, new Vector2(680f, 6f), new Vector2(0f, 192f),
            new Color(0.25f, 0.86f, 0.88f, 1f));

        CreateText("Title", windowRect, "SATÉLITE", new Vector2(580f, 58f),
            new Vector2(0f, 112f), 38f, FontStyles.Bold, new Color(0.8f, 0.98f, 1f, 1f));

        CreateText("Description", windowRect,
            "Transmissão recebida.\nEste painel é um modelo para a futura interação com o satélite.",
            new Vector2(580f, 112f), new Vector2(0f, 15f), 23f, FontStyles.Normal,
            new Color(0.86f, 0.92f, 0.97f, 1f));

        CreateText("Hint", windowRect, "Conteúdo em breve", new Vector2(520f, 36f),
            new Vector2(0f, -70f), 17f, FontStyles.Italic, new Color(0.48f, 0.77f, 0.82f, 1f));

        GameObject closeObject = CreateImage("CloseButton", windowRect, new Vector2(180f, 48f),
            new Vector2(0f, -145f), new Color(0.14f, 0.49f, 0.55f, 1f));
        Button closeButton = closeObject.AddComponent<Button>();
        closeButton.targetGraphic = closeObject.GetComponent<Image>();
        closeButton.onClick.AddListener(() => onClose?.Invoke());

        CreateText("CloseLabel", closeObject.GetComponent<RectTransform>(), "FECHAR",
            new Vector2(180f, 44f), Vector2.zero, 18f, FontStyles.Bold, Color.white);

        return overlay;
    }

    private static GameObject CreateImage(string objectName, Transform parent, Vector2 size,
        Vector2 position, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        imageObject.GetComponent<Image>().color = color;
        return imageObject;
    }

    private static void CreateText(string objectName, Transform parent, string content, Vector2 size,
        Vector2 position, float fontSize, FontStyles fontStyle, Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = content;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
    }
}
