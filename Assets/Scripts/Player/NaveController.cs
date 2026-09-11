using UnityEngine;
using System.Collections.Generic;

namespace Player
{
    public class NaveController : MonoBehaviour
    {
        [Header("Configurações de Movimento")]
        public float velocidade = 1f;
        public float velocidadeSprint = 5f;

        [Header("Configurações de Rotação (Espaço)")]
        [SerializeField]
        float multAnguloVelocidade = 0.5f;
        [SerializeField]
        float multVelocidadeRotacao = 0.05f;

        [Header("Configurações de Tiro")]
        [SerializeField]
        float tempoEntreTiros = 0.5f;
        float tempoUltimoTiro = 0f;
        [SerializeField]
        float danoDoTiro = 10f;
        [SerializeField]
        float distanciaDaMira = 500f;
        [SerializeField]
        float distanciaMinima = 20f;

        [Header("UI do Retículo")]
        [SerializeField]
        private RectTransform marcadorArmaUI;
        [SerializeField]
        private LayerMask layerColisaoMira;
        [SerializeField]
        [Tooltip("Margem de erro em pixels. Se o tiro desviar mais que isso do centro, o marcador aparece.")]
        private float limiteDesalinhamentoTela = 30f;

        [Header("Audios")]
        [SerializeField]
        private AudioClip tiroAudioClip;

        private Rigidbody rb;
        private StarterAssetsInputs input;
        private Camera cam;
        private AudioSource audioSource;

        [Space(10)]
        [SerializeField]
        List<ObjectPool> bulletPools;

        private void Awake()
        {
            cam = Camera.main;
            rb = GetComponent<Rigidbody>();
            input = GetComponent<StarterAssetsInputs>();
            audioSource = GetComponent<AudioSource>();
        }

        private void Update()
        {
            if (input.fire && Time.time > tempoUltimoTiro)
            {
                Atirar();
            }
        }

        private void LateUpdate()
        {
            AtualizarMarcadorUI();
        }

        // Centralizei a leitura do alvo da câmera para usar tanto no tiro quanto na UI
        private Vector3 ObterPontoAlvoCamera()
        {
            RaycastHit hit;
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            if (Physics.Raycast(ray, out hit, distanciaDaMira, layerColisaoMira))
            {
                if (hit.distance < distanciaMinima)
                    return ray.GetPoint(distanciaMinima);
                else
                    return hit.point;
            }

            return ray.GetPoint(distanciaDaMira);
        }

        private void Atirar()
        {
            Vector3 targetPoint = ObterPontoAlvoCamera();

            // Converte o alvo da câmera para as coordenadas relativas da nave
            Vector3 alvoLocalNave = transform.InverseTransformPoint(targetPoint);

            for (int i = 0; i < bulletPools.Count; i++)
            {
                GameObject bullet = bulletPools[i].GetInstance();
                Transform cano = bulletPools[i].transform;

                // Aqui é o pulo do gato: pegamos a posição do alvo, mas FORÇAMOS a posição 
                // horizontal (X) para ser exatamente a mesma do cano da arma.
                // Isso impede a arma de virar para os lados (Yaw), forçando ela a rotacionar
                // APENAS no eixo X (Pitch - para cima e para baixo).
                Vector3 canoLocalPos = transform.InverseTransformPoint(cano.position);
                Vector3 alvoEspecificoLocal = alvoLocalNave;
                alvoEspecificoLocal.x = canoLocalPos.x; // Trava convergência lateral

                // Converte de volta pro mundo real
                Vector3 finalTarget = transform.TransformPoint(alvoEspecificoLocal);
                Vector3 shootDirection = (finalTarget - cano.position).normalized;

                bullet.transform.position = cano.position;
                bullet.transform.rotation = Quaternion.LookRotation(shootDirection);

                Bullet bullet1 = bullet.GetComponent<Bullet>();
                if (bullet1 != null)
                {
                    bullet1.SetDamage(danoDoTiro);
                }

                bullet.SetActive(true);
            }
            audioSource.PlayOneShot(tiroAudioClip);
            tempoUltimoTiro = Time.time + tempoEntreTiros;
        }

        private void AtualizarMarcadorUI()
        {
            if (marcadorArmaUI == null) return;

            Vector3 targetPoint = ObterPontoAlvoCamera();
            Vector3 alvoLocalNave = transform.InverseTransformPoint(targetPoint);

            Vector3 centroDasArmas = Vector3.zero;
            if (bulletPools.Count > 0)
            {
                for (int i = 0; i < bulletPools.Count; i++)
                    centroDasArmas += bulletPools[i].transform.position;
                centroDasArmas /= bulletPools.Count;
            }
            else
            {
                centroDasArmas = transform.position;
            }

            Vector3 centroLocalPos = transform.InverseTransformPoint(centroDasArmas);
            Vector3 alvoMarcadorLocal = alvoLocalNave;
            alvoMarcadorLocal.x = centroLocalPos.x;

            Vector3 finalTargetMarcador = transform.TransformPoint(alvoMarcadorLocal);
            Vector3 direcaoRealTiro = (finalTargetMarcador - centroDasArmas).normalized;

            Vector3 pontoDeImpacto;
            RaycastHit hit;

            if (Physics.Raycast(centroDasArmas, direcaoRealTiro, out hit, distanciaDaMira, layerColisaoMira))
            {
                pontoDeImpacto = hit.point;
            }
            else
            {
                pontoDeImpacto = centroDasArmas + (direcaoRealTiro * distanciaDaMira);
            }

            // Posição do marcador na tela
            Vector3 screenPos = PosInScreen(pontoDeImpacto);

            // Posição do centro da tela (onde a crosshair principal fica)
            Vector2 centroDaTela = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            float distanciaDesalinhamentoVertical = Mathf.Abs(screenPos.y - centroDaTela.y);

            if (screenPos.z > 0 && distanciaDesalinhamentoVertical > limiteDesalinhamentoTela)
            {
                marcadorArmaUI.gameObject.SetActive(true);
                marcadorArmaUI.position = Vector3.Lerp(marcadorArmaUI.position, screenPos, 0.5f);
            }
            else
            {
                marcadorArmaUI.gameObject.SetActive(false);
            }
        }

        private void FixedUpdate()
        {
            float velocidadeAlvo = input.sprint ? velocidadeSprint : velocidade;

            rb.AddForce(rb.transform.TransformDirection(Vector3.forward) * input.move.y * velocidadeAlvo, ForceMode.VelocityChange);
            rb.AddForce(rb.transform.TransformDirection(Vector3.right) * input.move.x * velocidadeAlvo, ForceMode.VelocityChange);

            rb.AddTorque(rb.transform.right * multAnguloVelocidade * input.look.y * -1, ForceMode.VelocityChange);
            rb.AddTorque(rb.transform.up * multAnguloVelocidade * input.look.x, ForceMode.VelocityChange);

            rb.AddTorque(rb.transform.forward * multVelocidadeRotacao * -input.roll.x, ForceMode.VelocityChange);
        }

        Vector3 PosInScreen(Vector3 posWorld)
        {
            Vector3 posScreen = cam.WorldToScreenPoint(posWorld);

            if (cam.targetTexture != null)
            {
                posScreen.x *= (float)Screen.width / cam.targetTexture.width;
                posScreen.y *= (float)Screen.height / cam.targetTexture.height;
            }

            return posScreen;
        }
    }
}