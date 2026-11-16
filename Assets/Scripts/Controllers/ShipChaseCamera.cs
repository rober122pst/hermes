using Player;
using Unity.Cinemachine;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class ShipChaseCamera : CinemachineExtension
{
    [SerializeField] private NaveController pilot;
    [SerializeField, Min(4f)] private float distance = 35f;
    [SerializeField, Min(0f)] private float height = 7.5f;
    [SerializeField, Range(-10f, 10f)] private float pitch = 2f;
    [SerializeField] private LayerMask scenery = 1;
    [SerializeField, Range(0f, 12f)] private float cruiseFieldOfView = 6f;

    private float armLength;
    private float lensWidening;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Aim || pilot == null) return;

        Quaternion heading = pilot.ViewRotation;
        Vector3 forward = heading * Vector3.forward;
        Vector3 up = heading * Vector3.up;
        Vector3 pivot = pilot.transform.position + up * height;
        float availableDistance = distance;
        if (Application.isPlaying && Physics.SphereCast(pivot, 0.8f, -forward, out RaycastHit hit,
            distance, scenery, QueryTriggerInteraction.Ignore))
        {
            availableDistance = Mathf.Clamp(hit.distance - 0.35f, 0.8f, distance);
        }

        bool reset = !vcam.PreviousStateIsValid || deltaTime < 0f || armLength <= 0f;
        if (reset || availableDistance < armLength)
            armLength = availableDistance;
        else
            armLength = Mathf.Lerp(armLength, availableDistance, 1f - Mathf.Exp(-11f * deltaTime));

        state.RawPosition = pivot - forward * armLength;
        state.RawOrientation = heading * Quaternion.Euler(pitch, 0f, 0f);
        state.ReferenceUp = up;

        float cruiseRatio = pilot.IsCruising
            ? Mathf.InverseLerp(pilot.stats.SprintSpeed, pilot.MaxCruiseSpeed, pilot.TravelSpeed)
            : 0f;
        float targetWidening = Mathf.SmoothStep(0f, cruiseFieldOfView, cruiseRatio);
        lensWidening = reset ? targetWidening : Mathf.Lerp(lensWidening, targetWidening, 1f - Mathf.Exp(-7.5f * deltaTime));
        if (targetWidening == 0f && lensWidening < 0.02f)
            lensWidening = 0f;
        state.Lens.FieldOfView += lensWidening;
    }
}
