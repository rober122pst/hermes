using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace Player.UI
{
    public class AimRaycast : MonoBehaviour
    {
        [Header("Configurações Visuais")]
        public RectTransform cursorUI;
        public Animator animator;

        [Header("Configurações do Ímã e Drag")]
        [FormerlySerializedAs("objetosInterativos")]
        public LayerMask interactableLayers;
        [FormerlySerializedAs("raioDeDeteccao")]
        public float detectionRadius = 1.5f;
        [FormerlySerializedAs("velocidadeDoDrag")]
        public float dragSpeed = 15f; // Define a suavidade ao arrastar o objeto 3D.
        [FormerlySerializedAs("distanciaDaCamera")]
        public float cameraDistance = 70f;

        [Header("Configurações de Arremesso")]
        [FormerlySerializedAs("multiplicadorDeForca")]
        public float throwForceMultiplier = 1.2f; // Ajusta a agressividade do arremesso.
        public float shakeDuracao = 0.15f;
        public float shakeMagnitude = 5f;

        [Space(10)]
        public EnergyRay energyRay;

        private Camera cam;
        private Vector3 cursorInitialPos;
        private StarterAssetsInputs inputs;

        // Variáveis de controle de drag
        private Transform heldObject;
        private float dragDistance;
        private Rigidbody heldRigidbody;
        private bool wasKinematic;

        // Variáveis para rastrear a inércia do movimento
        private Vector3 previousPosition;
        private Vector3 movementVelocity;

        private bool canGrab;
        private bool grab;
        private bool blocked;

        private void Start()
        {
            cam = Camera.main;
            inputs = GetComponent<StarterAssetsInputs>();
            cursorInitialPos = cursorUI.position;
        }

        private void Update()
        {
            // O raio sempre sai do centro da tela (mira)
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            if (heldObject != null)
            {
                if (inputs.pull)
                {
                    DragObject(ray);
                }
                else
                {
                    ReleaseObject();
                }
            }
            else
            {
                FindObjects(ray);
            }

            animator.SetBool("isCanGrab", canGrab);
            animator.SetBool("isGrab", grab);
            animator.SetBool("isBlocked", blocked);
        }

        private void FindObjects(Ray ray)
        {
            RaycastHit hit;

            if (Physics.SphereCast(ray, detectionRadius, out hit, 100f, interactableLayers))
            {
                Vector3 screenPosition = WorldToScreenPosition(hit.transform.position);
                cursorUI.position = Vector2.Lerp(cursorUI.position, screenPosition, 0.2f);

                if (hit.distance > cameraDistance)
                {
                    blocked = false;
                    canGrab = true;
                    if (inputs.pull && heldObject == null) // Verifica se o botão de puxar foi pressionado e se não há objeto segurado.
                    {
                        GrabObject(hit);
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

        private void GrabObject(RaycastHit hit)
        {
            heldObject = hit.transform;
            dragDistance = hit.distance; // Salva a distância exata para arrastar no mesmo eixo Z.

            // Suspende a física para que o objeto não sofra influência de gravidade/colisão enquanto arrasta
            if (heldObject.TryGetComponent(out heldRigidbody))
            {
                wasKinematic = heldRigidbody.isKinematic;
                heldRigidbody.isKinematic = true;
            }
        }

        private void DragObject(Ray ray)
        {
            grab = true;
            // Trava o UI Cursor no objeto enquanto ele é movido
            Vector3 screenPosition = WorldToScreenPosition(heldObject.position);
            cursorUI.position = Vector3.Lerp(cursorUI.position, screenPosition, 0.6f);

            // Move o objeto 3D para o ponto projetado na frente da câmera
            Vector3 targetPosition = ray.GetPoint(dragDistance);
            heldObject.position = Vector3.Lerp(heldObject.position, targetPosition, Time.deltaTime * dragSpeed);

            // Calcula a velocidade do movimento do objeto segurado para efeitos de inércia
            movementVelocity = (heldObject.position - previousPosition) / Time.deltaTime;
            previousPosition = heldObject.position;

            energyRay.DrawEnergyArc(heldObject);
        }

        private void ReleaseObject()
        {
            energyRay.DisableArc();
            // Devolve as configurações originais de física para o objeto
            if (heldRigidbody != null)
            {
                heldRigidbody.isKinematic = wasKinematic;
                heldRigidbody.AddForce(movementVelocity * throwForceMultiplier, ForceMode.Impulse);

                heldObject = null;
            }

            heldObject = null;
            grab = false;
        }

        private Vector3 WorldToScreenPosition(Vector3 worldPosition)
        {
            Vector3 screenPosition = cam.WorldToScreenPoint(worldPosition);
            
            if (cam.targetTexture != null)
            {
                screenPosition.x *= (float)Screen.width / cam.targetTexture.width;
                screenPosition.y *= (float)Screen.height / cam.targetTexture.height;
            }

            return screenPosition;
        }
    }
}
