using Player;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float interactionDistance = 100f;
    [SerializeField] private float crosshairToleranceRadius = 4f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private NaveController playerController;

    private readonly Collider[] nearbyTerminals = new Collider[16];
    private IInteractive currentInteractable;
    private StarterAssetsInputs input;
    private InputAction jumpAction;

    private void Start()
    {
        input = GetComponent<StarterAssetsInputs>();
        if (playerController == null) playerController = GetComponent<NaveController>();
        if (TryGetComponent(out PlayerInput playerInput)) jumpAction = playerInput.actions?.FindAction("Jump");
        if (mainCamera == null) mainCamera = Camera.main;
    }

    private void Update()
    {
        if (playerController != null && !playerController.IsAlive)
        {
            Focus(null);
            return;
        }

        if (UIManager.Instance != null && UIManager.Instance.IsTacticallyPaused) return;
        Focus(FindInteractable());
        if (jumpAction?.WasPressedThisFrame() != true) return;

        switch (currentInteractable)
        {
            case RouteSatellite relay:
                relay.OpenPanel(input);
                break;
            case CoreTerminal terminal:
                terminal.OpenPanel(input);
                break;
            case Satellite satellite:
                satellite.OpenPanel(input);
                break;
        }
    }

    private IInteractive FindInteractable()
    {
        IInteractive nearby = FindNearbyTerminal();
        if (nearby != null || mainCamera == null) return nearby;

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.SphereCast(ray, crosshairToleranceRadius, out RaycastHit hit, interactionDistance,
            interactableLayer, QueryTriggerInteraction.Collide))
        {
            IInteractive focused = hit.collider.GetComponentInParent<IInteractive>();
            if (focused is Satellite satellite && satellite.isActiveAndEnabled) return focused;
        }
        return null;
    }

    private IInteractive FindNearbyTerminal()
    {
        float range = Mathf.Max(CoreTerminal.DockingDistance, RouteSatellite.DockingDistance);
        int count = Physics.OverlapSphereNonAlloc(transform.position, range, nearbyTerminals, interactableLayer,
            QueryTriggerInteraction.Collide);
        IInteractive closest = null;
        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            var terminal = nearbyTerminals[i].GetComponentInParent<IInteractive>();
            float distance = (nearbyTerminals[i].transform.position - transform.position).sqrMagnitude;
            if (distance >= closestDistance) continue;

            bool available = terminal switch
            {
                RouteSatellite relay => relay.CanFocus(transform),
                CoreTerminal core => core.CanFocus(transform),
                _ => false
            };
            if (!available) continue;
            closest = terminal;
            closestDistance = distance;
        }
        return closest;
    }

    private void Focus(IInteractive next)
    {
        if (currentInteractable == next) return;

        if (currentInteractable is Object previous && previous != null) currentInteractable.HideDialogueBox();
        currentInteractable = next;
        currentInteractable?.ShowDialogueBox();
    }

    private void OnDisable()
    {
        Focus(null);
    }
}
