using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Player.UI
{
    public class AimRaycast : MonoBehaviour
    {
        [Header("Configurações Visuais")]
        public RectTransform cursorUI;
        public Animator animator;

        [Header("Configurações do Ímã e Drag")]
        public LayerMask objetosInterativos;
        public float raioDeDeteccao = 1.5f;
        public float velocidadeDoDrag = 15f; // Define a suavidade ao arrastar o objeto 3D
        public float distanciaDaCamera = 70f;

        [Header("Configurações de Arremesso")]
        public float multiplicadorDeForca = 1.2f; // Ajusta a agressividade do arremesso
        public float shakeDuracao = 0.15f;
        public float shakeMagnitude = 5f;

        [Space(10)]
        public EnergyRay energyRay;

        private Camera cam;
        private Vector3 cursorInitialPos;
        private StarterAssetsInputs inputs;

        // Variáveis de controle de drag
        private Transform objetoSegurado;
        private float distanciaDoDrag;
        private Rigidbody rbSegurado;
        private bool rbEraKinematic;

        // Variáveis para rastrear a inércia do movimento
        private Vector3 posicaoAnterior;
        private Vector3 velocidadeDoMovimento;

        bool canGrab, grab, blocked;

        void Start()
        {
            cam = Camera.main;
            inputs = GetComponent<StarterAssetsInputs>();
            cursorInitialPos = cursorUI.position;
        }

        void Update()
        {
            // O raio sempre sai do centro da tela (mira)
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            if (objetoSegurado != null)
            {
                if (inputs.pull)
                {
                    ArrastarObjeto(ray);
                }
                else
                {
                    SoltarObjeto();
                }
            }
            else
            {
                ProcurarObjetos(ray);
            }

            animator.SetBool("isCanGrab", canGrab);
            animator.SetBool("isGrab", grab);
            animator.SetBool("isBlocked", blocked);
        }

        private void ProcurarObjetos(Ray ray)
        {
            RaycastHit hit;

            if (Physics.SphereCast(ray, raioDeDeteccao, out hit, 100f, objetosInterativos))
            {
                Vector3 posicaoNaTela = PosInScreen(hit.transform.position);
                cursorUI.position = Vector2.Lerp(cursorUI.position, posicaoNaTela, 0.2f);

                if (hit.distance > distanciaDaCamera)
                {
                    blocked = false;
                    canGrab = true;
                    if (inputs.pull && objetoSegurado == null) // Verifica se o botão de puxar foi pressionado e se não há objeto segurado
                    {
                        PegarObjeto(hit);
                    }
                }
                else
                {
                    blocked = true;
                }
            }
            else
            {
                blocked = false;
                canGrab = false;
                cursorUI.anchoredPosition = Vector2.Lerp(cursorUI.anchoredPosition, Vector2.zero, 0.2f);
            }
        }

        private void PegarObjeto(RaycastHit hit)
        {
            objetoSegurado = hit.transform;
            distanciaDoDrag = hit.distance; // Salva a distância exata para arrastar no mesmo eixo Z

            // Suspende a física para que o objeto não sofra influência de gravidade/colisão enquanto arrasta
            if (objetoSegurado.TryGetComponent(out rbSegurado))
            {
                rbEraKinematic = rbSegurado.isKinematic;
                rbSegurado.isKinematic = true;
            }
        }

        private void ArrastarObjeto(Ray ray)
        {
            grab = true;
            // Trava o UI Cursor no objeto enquanto ele é movido
            Vector3 posicaoNaTela = PosInScreen(objetoSegurado.position);
            cursorUI.position = Vector3.Lerp(cursorUI.position, posicaoNaTela, 0.6f);

            // Move o objeto 3D para o ponto projetado na frente da câmera
            Vector3 posicaoAlvo = ray.GetPoint(distanciaDoDrag);
            objetoSegurado.position = Vector3.Lerp(objetoSegurado.position, posicaoAlvo, Time.deltaTime * velocidadeDoDrag);

            // Calcula a velocidade do movimento do objeto segurado para efeitos de inércia
            velocidadeDoMovimento = (objetoSegurado.position - posicaoAnterior) / Time.deltaTime;
            posicaoAnterior = objetoSegurado.position;

            energyRay.DesenharArcoDeEnergia(objetoSegurado.transform);
        }

        private void SoltarObjeto()
        {
            energyRay.DesativarArco();
            // Devolve as configurações originais de física para o objeto
            if (rbSegurado != null)
            {
                rbSegurado.isKinematic = rbEraKinematic;
                rbSegurado.AddForce(velocidadeDoMovimento * multiplicadorDeForca, ForceMode.Impulse);

                objetoSegurado = null;
            }

            objetoSegurado = null;
            grab = false;
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