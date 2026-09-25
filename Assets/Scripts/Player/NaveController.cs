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
        public Transform rayOrigin;

        public Camera mainCamera;

        public RectTransform uiDot;

        public float maxDistance = 100f;

        public LayerMask collisionMask;

        [Header("Audios")]
        [SerializeField]
        private AudioClip tiroAudioClip;

        private Rigidbody rb;
        private StarterAssetsInputs input;
        private Camera cam;
        private AudioSource audioSource;

        [Space(10)]
        [SerializeField]
        List<GameObject> weapons;
        public WeaponManager weaponManager;
        public WeaponData weaponData;

        private void Awake()
        {
            cam = Camera.main;
            rb = GetComponent<Rigidbody>();
            input = GetComponent<StarterAssetsInputs>();
            audioSource = GetComponent<AudioSource>();
            weaponManager.EquipWeapon(weaponData);
        }

        // private void Update()
        // {
        //     if (input.fire && Time.time > tempoUltimoTiro)
        //     {
        //         Atirar();
        //     }
        // }

        private void LateUpdate()
        {
            CrosshairUpdate();
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

        void CrosshairUpdate()
        {
            if (rayOrigin == null || mainCamera == null || uiDot == null) return;

            // Cria o raio saindo da origem e indo para a frente do objeto
            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            Vector3 targetPosition;

            // Lança o raycast
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, collisionMask))
            {
                // Se houver colisão, o alvo é exatamente o ponto onde bateu
                targetPosition = hit.point;
                Debug.DrawLine(hit.point, hit.point + Vector3.up * 2f, Color.yellow);
                Debug.Log("Não mirando");
            }
            else
            {
                // Se não bater em nada, o alvo é o limite máximo da distância no ar
                targetPosition = rayOrigin.position + rayOrigin.forward * maxDistance;
                Debug.DrawLine(rayOrigin.position, rayOrigin.position + Vector3.up * 2f, Color.red);
                Debug.Log("Não mirando");
            }

            // Converte a coordenada 3D do mundo em uma coordenada 2D de tela em pixels
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(targetPosition);

            // O valor Z da posição de tela indica se o ponto está na frente ou atrás da câmera
            if (screenPosition.z > 0)
            {
                uiDot.gameObject.SetActive(true);
                // Move a UI para a coordenada da tela. 
                // Obs: O Canvas deve estar configurado como "Screen Space - Overlay".
                uiDot.position = screenPosition;
            }
            else
            {
                // Esconde a bolinha da UI caso o ponto fique atrás do jogador/câmera
                uiDot.gameObject.SetActive(false);
            }
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