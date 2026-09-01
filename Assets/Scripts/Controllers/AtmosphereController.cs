using UnityEngine;

// ------------------------------------------------------------------
// Coloque este script no GameObject que tem o MeshRenderer da "casca"
// de atmosfera (uma esfera com raio um pouco maior que o planeta,
// ex.: planeta = 5000 -> atmosfera = 5250, ~5% a mais).
//
// O shader precisa do centro do planeta em World Space atualizado
// todo frame (caso o planeta se mova), então isso não pode ser um
// valor fixo no material — precisa ser setado via script.
// ------------------------------------------------------------------
[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public class AtmosphereController : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Transform do planeta (esfera sólida). Se vazio, usa o próprio transform deste objeto.")]
    public Transform planetTransform;

    [Header("Escala (mundo grande: planeta = 5000)")]
    public float planetRadius = 5000f;
    [Tooltip("Normalmente 2%~8% maior que o raio do planeta.")]
    public float atmosphereRadius = 5250f;

    [Header("Wavelengths (nm) — controla a cor do céu")]
    [Range(380, 780)] public float wavelengthR = 700f;
    [Range(380, 780)] public float wavelengthG = 530f;
    [Range(380, 780)] public float wavelengthB = 440f;

    [Header("Scattering")]
    public float scatteringStrength = 20f;
    [Range(0.1f, 20f)] public float densityFalloff = 8f;
    public float mieCoefficient = 4f;
    [Range(-0.999f, 0.999f)] public float mieG = 0.76f;
    public float sunIntensity = 20f;

    [Header("Performance (mantenha baixo)")]
    [Range(2, 16)] public int inScatteringSteps = 8;
    [Range(2, 8)] public int opticalDepthSteps = 4;

    private static readonly int PlanetCenterID = Shader.PropertyToID("_PlanetCenter");
    private static readonly int PlanetRadiusID = Shader.PropertyToID("_PlanetRadius");
    private static readonly int AtmosphereRadiusID = Shader.PropertyToID("_AtmosphereRadius");
    private static readonly int WaveLengthRID = Shader.PropertyToID("_WaveLengthR");
    private static readonly int WaveLengthGID = Shader.PropertyToID("_WaveLengthG");
    private static readonly int WaveLengthBID = Shader.PropertyToID("_WaveLengthB");
    private static readonly int ScatteringStrengthID = Shader.PropertyToID("_ScatteringStrength");
    private static readonly int DensityFalloffID = Shader.PropertyToID("_DensityFalloff");
    private static readonly int MieCoefficientID = Shader.PropertyToID("_MieCoefficient");
    private static readonly int MieGID = Shader.PropertyToID("_MieG");
    private static readonly int SunIntensityID = Shader.PropertyToID("_SunIntensity");
    private static readonly int InScatteringPointsID = Shader.PropertyToID("_InScatteringPoints");
    private static readonly int OpticalDepthPointsID = Shader.PropertyToID("_OpticalDepthPoints");

    private MeshRenderer _renderer;
    private MaterialPropertyBlock _mpb;

    private void OnEnable()
    {
        _renderer = GetComponent<MeshRenderer>();
        _mpb = new MaterialPropertyBlock();
    }

    // Usa MaterialPropertyBlock em vez de material.Set* diretamente:
    // evita criar uma instância de material por objeto (menos garbage,
    // permite instancing/batching continuar funcionando).
    private void LateUpdate()
    {
        if (_renderer == null) return;

        Transform planet = planetTransform != null ? planetTransform : transform;

        _renderer.GetPropertyBlock(_mpb);

        _mpb.SetVector(PlanetCenterID, planet.position);
        _mpb.SetFloat(PlanetRadiusID, planetRadius);
        _mpb.SetFloat(AtmosphereRadiusID, atmosphereRadius);

        _mpb.SetFloat(WaveLengthRID, wavelengthR);
        _mpb.SetFloat(WaveLengthGID, wavelengthG);
        _mpb.SetFloat(WaveLengthBID, wavelengthB);

        _mpb.SetFloat(ScatteringStrengthID, scatteringStrength);
        _mpb.SetFloat(DensityFalloffID, densityFalloff);
        _mpb.SetFloat(MieCoefficientID, mieCoefficient);
        _mpb.SetFloat(MieGID, mieG);
        _mpb.SetFloat(SunIntensityID, sunIntensity);

        _mpb.SetInt(InScatteringPointsID, inScatteringSteps);
        _mpb.SetInt(OpticalDepthPointsID, opticalDepthSteps);

        _renderer.SetPropertyBlock(_mpb);
    }

    private void OnDrawGizmosSelected()
    {
        Transform planet = planetTransform != null ? planetTransform : transform;
        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.3f);
        Gizmos.DrawWireSphere(planet.position, atmosphereRadius);
        Gizmos.color = new Color(0.5f, 0.4f, 0.2f, 0.5f);
        Gizmos.DrawWireSphere(planet.position, planetRadius);
    }
}
