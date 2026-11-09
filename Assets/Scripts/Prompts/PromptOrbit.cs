using Player;
using UnityEngine;

public class PromptOrbit : MonoBehaviour
{
    private Transform targetObject;
    private Transform cameraTransform;

    [Tooltip("Opcional: CanvasGroup para aplicar fade de transparência junto com a escala")]
    public CanvasGroup canvasGroup;

    [Header("Configurações de Posição e Órbita")]
    [Tooltip("X = Lado da tela | Y = Altura | Z = Distância à frente do objeto em direção à câmera")]
    public Vector3 baseOffset = new Vector3(0.6f, 0.5f, 0.4f);

    [Tooltip("Mantém o eixo X e Z relativos à visão da câmera para nunca ficar atrás do objeto")]
    public bool useCameraRelativeOffset = true;

    [Tooltip("Tempo de suavização ao seguir o movimento (cria o efeito de atraso elástico)")]
    public float smoothTime = 0.15f;

    [Header("Efeito de Flutuação")]
    [Tooltip("Velocidade da flutuação vertical")]
    public float floatFrequency = 2f;

    [Tooltip("Altura máxima da flutuação vertical")]
    public float floatAmplitude = 0.08f;

    [Header("Atração da Câmera")]
    [Tooltip("Força adicional com que o prompt é puxado para perto da câmera")]
    [Range(0f, 1f)]
    public float cameraPullStrength = 0.2f;

    [Tooltip("Distância máxima que o puxão da câmera pode exercer")]
    public float maxPullDistance = 0.5f;

    [Header("Rotação (Billboard)")]
    [Tooltip("Velocidade com que o prompt gira para encarar a câmera")]
    public float rotationSpeed = 12f;

    [Tooltip("Travar rotação apenas no eixo Y")]
    public bool lockYAxisOnly = false;

    [Header("Animação de Surgimento e Desaparecimento")]
    [Tooltip("Ativa ou desativa o prompt automaticamente conforme a distância da câmera/jogador")]
    public bool autoToggleByDistance = true;

    [Tooltip("Distância máxima para o prompt aparecer")]
    public float showDistance = 4.5f;

    [Tooltip("Velocidade da animação de aparecer e sumir")]
    public float transitionSpeed = 6f;

    [Tooltip("Curva da animação de escala (permite criar efeito elástico no surgimento)")]
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Vector3 currentVelocity;
    private Vector3 initialScale;
    private float visibilityProgress;
    private bool isVisible;

    public void Setup(Transform target, Transform camera = null)
    {
        targetObject = target;
        cameraTransform = camera ?? Camera.main?.transform;
    }

    private void Awake()
    {
        initialScale = transform.localScale;

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (targetObject == null && transform.parent != null)
        {
            targetObject = transform.parent;
        }

        // Inicia invisível caso seja controlado por distância
        if (autoToggleByDistance)
        {
            visibilityProgress = 0f;
            ApplyVisibilityVisuals(0f);
        }
        else
        {
            isVisible = true;
            visibilityProgress = 1f;
            ApplyVisibilityVisuals(1f);
        }
    }

    private void LateUpdate()
    {
        if (targetObject == null || cameraTransform == null) return;

        UpdateVisibilityState();

        // Otimização: só calcula física de posição e rotação se estiver visível ou animando
        if (visibilityProgress > 0.001f || isVisible)
        {
            UpdatePosition();
            UpdateRotation();
        }
    }

    private void UpdateVisibilityState()
    {
        if (autoToggleByDistance)
        {
            float distanceToCamera = Vector3.Distance(cameraTransform.position, targetObject.position);
            isVisible = distanceToCamera <= showDistance;
        }

        float targetAlpha = isVisible ? 1f : 0f;
        visibilityProgress = Mathf.MoveTowards(visibilityProgress, targetAlpha, Time.deltaTime * transitionSpeed);

        ApplyVisibilityVisuals(visibilityProgress);
    }

    private void ApplyVisibilityVisuals(float progress)
    {
        float curveValue = scaleCurve.Evaluate(progress);
        transform.localScale = initialScale * curveValue;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = progress;
        }
    }

    private void UpdatePosition()
    {
        // Direção do objeto até a câmera
        Vector3 directionToCamera = cameraTransform.position - targetObject.position;
        Vector3 dirNormalized = directionToCamera.normalized;

        Vector3 calculatedOffset;

        if (useCameraRelativeOffset)
        {
            // Eixo X segue a direita da câmera, Y sobe no mundo, Z puxa para a frente do objeto (na direção da câmera)
            Vector3 rightOffset = cameraTransform.right * baseOffset.x;
            Vector3 upOffset = Vector3.up * baseOffset.y;
            Vector3 forwardOffset = dirNormalized * baseOffset.z;

            calculatedOffset = rightOffset + upOffset + forwardOffset;
        }
        else
        {
            calculatedOffset = baseOffset;
        }

        Vector3 anchorPosition = targetObject.position + calculatedOffset;

        // Flutuação vertical contínua
        float verticalFloat = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        Vector3 floatOffset = Vector3.up * verticalFloat;

        // Atração magnética extra em direção à câmera
        Vector3 cameraPull = Vector3.ClampMagnitude(directionToCamera * cameraPullStrength, maxPullDistance);

        // Posição final suavizada
        Vector3 desiredPosition = anchorPosition + floatOffset + cameraPull;

        // Quando o prompt acaba de começar a aparecer, posiciona direto para evitar que venha "voando" de longe
        if (visibilityProgress <= 0.02f && isVisible)
        {
            transform.position = desiredPosition;
            currentVelocity = Vector3.zero;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);
        }
    }

    private void UpdateRotation()
    {
        Vector3 lookDirection = transform.position - cameraTransform.position;

        if (lockYAxisOnly)
        {
            lookDirection.y = 0f;
        }

        if (lookDirection.sqrMagnitude != 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    // Métodos públicos caso queira controlar via Trigger ou Raycast de interação
    public void Show()
    {
        autoToggleByDistance = false;
        isVisible = true;
    }

    public void Hide()
    {
        autoToggleByDistance = false;
        isVisible = false;
    }

    public void SetVisible(bool visibleState)
    {
        autoToggleByDistance = false;
        isVisible = visibleState;
    }
}