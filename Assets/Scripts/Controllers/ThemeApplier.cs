using UnityEngine;
using UnityEngine.UI;

// O atributo [ExecuteAlways] faz o script rodar no modo de edição (Scene View)
[ExecuteAlways]
[RequireComponent(typeof(Graphic))]
public class ThemeApplier : MonoBehaviour
{
    public ColorTheme currentTheme;

    // Tipos de cores disponíveis no tema
    public enum ThemeColorType
    {
        Primary,
        Secondary,
        Background
    }

    // Seleção de qual cor este elemento específico deve usar
    public ThemeColorType colorType;

    private Graphic uiElement;

    private void Start()
    {
        Initialize();
    }

    // O método Update garante a atualização em tempo real quando você altera o .asset no Editor
    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            ApplyColor();
        }
#endif
    }

    // Inicializa o componente gráfico (Image, Text, TextMeshProUGUI, etc)
    private void Initialize()
    {
        if (uiElement == null)
        {
            uiElement = GetComponent<Graphic>();
        }
    }

    public void ApplyColor()
    {
        Initialize();

        if (currentTheme == null || uiElement == null) return;

        // Aplica a cor com base na escolha feita no Inspector
        switch (colorType)
        {
            case ThemeColorType.Primary:
                uiElement.color = currentTheme.primaryColor;
                break;
            case ThemeColorType.Secondary:
                uiElement.color = currentTheme.secondaryColor;
                break;
            case ThemeColorType.Background:
                uiElement.color = currentTheme.backgroundColor;
                break;
        }
    }
}