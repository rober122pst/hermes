using Unity.Cinemachine;
using UnityEngine;

public class CinemachineShake : CinemachineExtension
{
    public static CinemachineShake Instance { get; private set; }
    private float startTime;
    private float duration;
    private float distance;

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    protected override void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        base.OnDestroy();
    }

    public void ShakeCamera(float intensity, float duration)
    {
        if (!float.IsFinite(intensity) || !float.IsFinite(duration) || intensity <= 0f || duration <= 0f)
            return;

        float requestedDistance = Mathf.Min(intensity, 0.45f);
        if (requestedDistance < CurrentOffset())
            return;

        distance = requestedDistance;
        this.duration = duration;
        startTime = Time.unscaledTime;
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Finalize)
            return;

        Vector3 forward = state.GetCorrectedOrientation() * Vector3.forward;
        state.PositionCorrection -= forward * CurrentOffset();
    }

    private float CurrentOffset()
    {
        if (duration <= 0f) return 0f;

        float remaining = 1f - Mathf.Clamp01((Time.unscaledTime - startTime) / duration);
        return distance * remaining * remaining;
    }
}
