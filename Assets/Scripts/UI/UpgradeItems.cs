using System.Collections.Generic;
using Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UpgradeItems : MonoBehaviour
{
    public BuyItemButton[] buttons;
    public StarterAssetsInputs inputs;

    private readonly Dictionary<WeaponData, HermesPatch> patches = new();
    private List<WeaponData> offeredWeapons;
    private HermesPatchConsole console;
    private TextMeshProUGUI prompt;

    private void Reset()
    {
        buttons = GetComponentsInChildren<BuyItemButton>();
    }

    private void OnEnable()
    {
        UIManager manager = UIManager.Instance;
        if (manager == null || WeaponManager.Instance == null)
        {
            gameObject.SetActive(false);
            return;
        }

        if (!manager.OwnsTacticalPause(gameObject) && !manager.TryBeginTacticalPause(gameObject, inputs))
        {
            gameObject.SetActive(false);
            return;
        }

        patches.Clear();
        offeredWeapons = WeaponManager.Instance.GetRandomWeapons();
        if (offeredWeapons.Count == 0)
        {
            gameObject.SetActive(false);
            return;
        }

        if (prompt == null) CreatePrompt();
        ShowOffers();
    }

    private void ShowOffers()
    {
        prompt.gameObject.SetActive(true);
        prompt.text = "TERMINAL DE BORDO\nSelecione um módulo para calibrar";

        for (int i = 0; i < buttons.Length; i++)
        {
            bool visible = i < offeredWeapons.Count;
            buttons[i].gameObject.SetActive(visible);
            if (!visible) continue;

            WeaponData offer = offeredWeapons[i];
            int level = WeaponManager.Instance.GetLevelByName(offer.weaponName);
            string description = level > 0 ? offer.updates.weaponLevels[level - 1].description : "Equipar no espaço livre.";
            buttons[i].Setup(offer.weaponName, description, level + 1, () => OpenPatch(offer, level + 1));
        }

        if (EventSystem.current != null && buttons.Length > 0 && buttons[0].gameObject.activeSelf)
            EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);
    }

    private void OpenPatch(WeaponData weapon, int nextLevel)
    {
        if (console != null) return;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        prompt.gameObject.SetActive(false);
        foreach (BuyItemButton button in buttons) button.gameObject.SetActive(false);

        if (!patches.TryGetValue(weapon, out HermesPatch patch))
        {
            patch = HermesPatches.CalibrateModule(weapon.weaponName, nextLevel, () =>
            {
                WeaponManager.Instance.EquipWeapon(weapon);
                gameObject.SetActive(false);
            });
            patches.Add(weapon, patch);
        }

        console = HermesPatchConsole.Open((RectTransform)canvas.transform, patch, "Voltar", applying =>
        {
            console = null;
            if (!applying && isActiveAndEnabled) ShowOffers();
        });
    }

    private void CreatePrompt()
    {
        GameObject heading = new GameObject("Módulos de bordo", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = heading.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchoredPosition = new Vector2(0f, 127f);
        rect.sizeDelta = new Vector2(650f, 125f);
        prompt = heading.GetComponent<TextMeshProUGUI>();
        prompt.fontSize = 24f;
        prompt.enableAutoSizing = true;
        prompt.fontSizeMin = 15f;
        prompt.fontSizeMax = 24f;
        prompt.fontStyle = FontStyles.Bold;
        prompt.color = new Color(0.85f, 0.98f, 1f);
        prompt.alignment = TextAlignmentOptions.Center;
        prompt.richText = false;
        prompt.raycastTarget = false;
        heading.AddComponent<UnityEngine.UI.Outline>().effectColor = new Color(0.01f, 0.03f, 0.09f, 1f);
    }

    private void OnDisable()
    {
        if (console != null) console.Close(true);
        if (UIManager.Instance != null) UIManager.Instance.EndTacticalPause(gameObject);
    }
}
