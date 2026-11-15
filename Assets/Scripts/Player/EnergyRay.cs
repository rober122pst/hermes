using UnityEngine;
using UnityEngine.Serialization;

public class EnergyRay : MonoBehaviour
{
    public static EnergyRay Instance { get; private set; }

    [Header("Visual do Arco de Energia")]
    [FormerlySerializedAs("raioRenderer")]
    public LineRenderer arcRenderer;
    private Transform arcOrigin;
    [FormerlySerializedAs("segmentosDoRaio")]
    public int arcSegmentCount = 20;
    [FormerlySerializedAs("alturaDoArco")]
    public float arcHeight = 0.2f; // Curvatura principal.
    [FormerlySerializedAs("oscilacaoEletrica")]
    public float electricalOscillation = 0.3f; // Força do "choque" contorcido.
    [FormerlySerializedAs("velocidadeOscilacao")]
    public float oscillationSpeed = 25f; // Velocidade da vibração da energia.
    public GameObject beam;

    // Inicializa o componente antes do primeiro quadro.
    private void Awake()
    {
        arcOrigin = transform;
        if (arcRenderer != null)
        {
            arcRenderer.positionCount = arcSegmentCount;
            arcRenderer.enabled = false;
            beam.SetActive(false);
        }
    }

    private void Start()
    {
        Instance = this;
    }

    // Atualiza o efeito a cada quadro.
    public void DrawEnergyArc(Transform target)
    {
        arcRenderer.enabled = true;
        beam.SetActive(true);

        Vector3 origin = arcOrigin.position;
        Vector3 destination = target.position;

        for (int i = 0; i < arcSegmentCount; i++)
        {
            float interpolation = (float)i / (arcSegmentCount - 1);

            // Interpola linearmente entre a origem e o destino.
            Vector3 basePoint = Vector3.Lerp(origin, destination, interpolation);

            // Cria um arco que começa e termina nas extremidades do raio.
            float arcFactor = Mathf.Sin(interpolation * Mathf.PI);

            // Aplica a curvatura principal do arco.
            Vector3 arcCurve = Vector3.up * (arcHeight * arcFactor);

            // Atualiza a posição do segmento atual.
            arcRenderer.SetPosition(i, basePoint + arcCurve);
        }
    }

    public void DisableArc()
    {
        arcRenderer.enabled = false;
        beam.SetActive(false);
    }
}
