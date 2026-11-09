using UnityEngine;
using TMPro;
using Player; // Remova se usar o Text legado da Unity

public class PlayerInteractor : MonoBehaviour
{
    [Header("Configuracoes da Mira e Raycast")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float interactionDistance = 3.5f;
    [Tooltip("Raio de tolerancia para detectar objetos proximos a mira")]
    [SerializeField] private float crosshairToleranceRadius = 0.35f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Controles e Referencias")]
    [SerializeField] private NaveController playerController;

    // Guarda a referencia atual para evitar chamadas desnecessarias na UI
    private IInteractive currentInteractable;
    private RaycastHit hitInfo;

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        ClearInteractable();
    }

    private void Update()
    {
        CheckForInteractable();
    }

    private void CheckForInteractable()
    {
        // Cria um raio saindo exatamente do centro da tela (posicao da mira)
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // O SphereCast projeta uma esfera ao longo do raio, facilitando a deteccao proxima a mira
        if (Physics.SphereCast(ray, crosshairToleranceRadius, out hitInfo, interactionDistance, interactableLayer))
        {
            if (hitInfo.collider.TryGetComponent(out IInteractive interactive))
            {
                // Atualiza a UI apenas se o objeto focado mudou
                if (currentInteractable != interactive)
                {
                    Debug.Log($"Objeto interativo detectado: {interactive}");
                    currentInteractable = interactive;
                    currentInteractable.ShowDialogueBox();
                }
                return;
            }
        }

        // Se o raio nao atingir nada valido, limpa o estado atual
        ClearInteractable();
    }

    private void ClearInteractable()
    {
        if (currentInteractable != null)
        {
            currentInteractable.HideDialogueBox();
            currentInteractable = null;
        }
    }

    // Desenha o raio e a esfera de tolerancia no editor para facilitar o ajuste
    private void OnDrawGizmosSelected()
    {
        if (mainCamera == null) return;

        Gizmos.color = currentInteractable != null ? Color.green : Color.yellow;

        Vector3 origin = mainCamera.transform.position;
        Vector3 direction = mainCamera.transform.forward;
        Vector3 endPosition = origin + (direction * interactionDistance);

        Gizmos.DrawLine(origin, endPosition);
        Gizmos.DrawWireSphere(endPosition, crosshairToleranceRadius);
    }
}