using UnityEngine;

public class EnergyRay : MonoBehaviour
{
    public static EnergyRay Instance { get; private set; }

    [Header("Visual do Arco de Energia")]
    public LineRenderer raioRenderer;
    Transform origemDoRaio;
    public int segmentosDoRaio = 20;
    public float alturaDoArco = 0.2f; // Curvatura principal
    public float oscilacaoEletrica = 0.3f; // Força do "choque" contorcido
    public float velocidadeOscilacao = 25f; // Quão rápido a energia vibra
    public GameObject beam;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        origemDoRaio = gameObject.transform;
        if (raioRenderer != null)
        {
            raioRenderer.positionCount = segmentosDoRaio;
            raioRenderer.enabled = false;
            beam.SetActive(false);
        }
    }

    void Start()
    {
        Instance = this;
    }

    // Update is called once per frame
    public void DesenharArcoDeEnergia(Transform destinoDoRaio)
    {
        raioRenderer.enabled = true;
        beam.SetActive(true);

        Vector3 origem = origemDoRaio.position;
        Vector3 destino = destinoDoRaio.position;

        for (int i = 0; i < segmentosDoRaio; i++)
        {
            float t = (float)i / (segmentosDoRaio - 1);

            // Interpolamos linearmente do ponto A ao ponto B
            Vector3 pontoBase = Vector3.Lerp(origem, destino, t);

            // O fatorContorno cria um arco (0 nas pontas, 1 no meio) para travar o raio nos objetos
            float fatorContorno = Mathf.Sin(t * Mathf.PI);

            // Curva gravitacional base
            Vector3 curvaArco = Vector3.up * (alturaDoArco * fatorContorno);

            // Aplica tudo ao ponto atual
            raioRenderer.SetPosition(i, pontoBase + curvaArco);
        }
    }

    public void DesativarArco()
    {
        raioRenderer.enabled = false;
        beam.SetActive(false);
    }
}
