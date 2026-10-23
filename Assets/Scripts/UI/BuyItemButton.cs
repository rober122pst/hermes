using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

[RequireComponent(typeof(Button))]
public class BuyItemButton : MonoBehaviour
{
    [Header("Referencias Visuais")]
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI levelText;

    // Executa no Editor ao adicionar o script ou clicar em 'Reset' no Inspector
    private void Reset()
    {
        button = GetComponent<Button>();
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>();
        labelText = texts[0];
        descriptionText = texts[1];
        levelText = texts[2];
    }

    public void Setup(string label, string description, int level, UnityAction onClickAction)
    {
        if (labelText != null)
        {
            labelText.text = label;
        }

        if (descriptionText != null)
        {
            descriptionText.text = description;
        }

        if (levelText != null)
        {
            levelText.text = "Level: " + level;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (onClickAction != null)
            {
                button.onClick.AddListener(onClickAction);
            }
        }
    }

    public void RemoveListener()
    {
        button.onClick.RemoveAllListeners();
    }

    public void SetInteractable(bool state)
    {
        button.interactable = state;
    }
}