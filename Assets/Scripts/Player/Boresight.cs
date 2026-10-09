using UnityEngine;

public class Boresight : MonoBehaviour
{
    [Header("UI do Retículo")]
    public Transform rayOrigin;
    public Camera mainCamera;
    public RectTransform uiDot;
    public float maxDistance = 100f;
    public LayerMask collisionMask;
    public RectTransform canvasRect;

    [Header("Configuração de Oclusão")]
    public CanvasGroup reticleCanvasGroup;

    private Vector3 initialPosition;

    void Start()
    {
        initialPosition = uiDot.localPosition;
    }

    void FixedUpdate()
    {
        CrosshairUpdate();
    }

    void CrosshairUpdate()
    {
        if (rayOrigin == null || mainCamera == null || uiDot == null) return;

        // Cria o raio saindo da origem e indo para a frente do objeto
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        Vector3 targetPosition;

        // Lança o raycast da arma
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, collisionMask))
        {
            targetPosition = hit.point;
        }
        else
        {
            uiDot.localPosition = initialPosition;
            return;
        }

        // Converte a coordenada 3D do mundo em uma coordenada 2D de tela em pixels
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(targetPosition);
        Vector2 uiLocalPos;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            mainCamera,
            out uiLocalPos
        );

        // O valor Z da posição de tela indica se o ponto está na frente ou atrás da câmera
        if (screenPosition.z > 0)
        {
            uiDot.localPosition = uiLocalPos;
        }
        else
        {
            uiDot.localPosition = initialPosition;
        }
    }
}