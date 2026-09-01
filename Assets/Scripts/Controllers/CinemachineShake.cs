using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class CinemachineShake : MonoBehaviour
{
    public static CinemachineShake Instance { get; private set; }
    private CinemachineCamera cinemachineCamera;
    private CinemachineBasicMultiChannelPerlin perlinNoise;

    void Awake()
    {
        Instance = this;
        cinemachineCamera = GetComponent<CinemachineCamera>();
        perlinNoise = cinemachineCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
    }

    public void ShakeCamera(float intensidade, float tempo)
    {
        perlinNoise.AmplitudeGain = intensidade;
        StartCoroutine(WaitTime(tempo));
    }

    IEnumerator WaitTime(float shakeTime)
    {
        yield return new WaitForSeconds(shakeTime);
        ResetIntensity();
    }

    void ResetIntensity()
    {
        perlinNoise.AmplitudeGain = 0f;
    }
}
