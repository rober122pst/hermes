using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [Header("Configurações de Animação")]
    public float fadeDuration = 1.2f;
    public Vector2 moveSpeed = new Vector2(1f, 2f); // X (Direita) e Y (Cima)

    [Header("Cores por Dano")]
    public Color lowDamageColor = Color.white;
    public Color mediumDamageColor = Color.yellow;
    public Color highDamageColor = new Color(1f, 0.5f, 0f); // Laranja
    public Color extremeDamageColor = Color.red;

    [Header("Limites de Cor")]
    public float mediumThreshold = 50f;
    public float highThreshold = 200f;
    public float extremeThreshold = 1000f;

    private TextMeshPro textMesh;
    private float currentDamage;
    private float fadeTimer;
    private Color currentColor;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    // Chamado na primeira vez que o dano é instanciado
    public void Setup(float damage)
    {
        currentDamage = damage;
        fadeTimer = fadeDuration;
        UpdateVisuals();
    }

    // Chamado se o alvo tomar dano novamente enquanto este popup ainda existir
    public void AddDamage(float additionalDamage)
    {
        currentDamage += additionalDamage;

        // Reseta o tempo de vida e dá um leve pulo para indicar o novo hit
        fadeTimer = fadeDuration;
        transform.localScale = Vector3.one * 1.3f;

        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        textMesh.text = FormatDamage(currentDamage);
        currentColor = GetColorForDamage(currentDamage);
        textMesh.color = currentColor;
    }

    void Update()
    {
        // Movimento para cima e para a direita
        transform.position += new Vector3(moveSpeed.x, moveSpeed.y, 0) * Time.deltaTime;

        // Suaviza a escala de volta ao normal caso tenha sido somado dano
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime * 10f);

        // Lógica de Fade (vai sumindo)
        fadeTimer -= Time.deltaTime;
        float alpha = fadeTimer / (fadeDuration * 0.5f); // Começa a sumir na metade do tempo

        Color fadeColor = currentColor;
        fadeColor.a = Mathf.Clamp01(alpha);
        textMesh.color = fadeColor;

        // Destrói o objeto quando some totalmente
        if (fadeTimer <= 0)
        {
            Destroy(gameObject);
        }
    }

    // Abreviador de números gigantes (1K, 1.5M, etc)
    private string FormatDamage(float amount)
    {
        if (amount >= 1000000000) return (amount / 1000000000f).ToString("0.#") + "B";
        if (amount >= 1000000) return (amount / 1000000f).ToString("0.#") + "M";
        if (amount >= 1000) return (amount / 1000f).ToString("0.#") + "K";

        return amount.ToString("0"); // Dano normal sem casas decimais
    }

    private Color GetColorForDamage(float amount)
    {
        if (amount >= extremeThreshold) return extremeDamageColor;
        if (amount >= highThreshold) return highDamageColor;
        if (amount >= mediumThreshold) return mediumDamageColor;
        return lowDamageColor;
    }
}