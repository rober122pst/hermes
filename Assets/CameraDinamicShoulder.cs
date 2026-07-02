using UnityEngine;
using Unity.Cinemachine; // Use Cinemachine se for versão mais antiga

public class CameraDinamicShoulder : MonoBehaviour
{
public CinemachineCamera vcam; // Sua câmera virtual
private CinemachineThirdPersonFollow thirdPersonFollow;

public float maxDistance = 2.2f;
public float minDistance = 0.5f; // Distância crítica de colisão

void Start()
{
    if (vcam != null)
    {
        thirdPersonFollow = vcam.GetComponent<CinemachineThirdPersonFollow>();
    }
}

void Update()
{
    if (thirdPersonFollow == null) return;

    // O Cinemachine ajusta internamente a posição da câmera baseado na colisão.
    // Você pode medir a distância atual do "LookAt" ou alvo até a câmera real do jogo.
    float currentDistance = Vector3.Distance(Camera.main.transform.position, vcam.Follow.position);

    // Calcula um valor de 0 a 1 baseado na proximidade
    float t = Mathf.InverseLerp(minDistance, maxDistance, currentDistance);

    // Se a distância for máxima (t = 1), CameraSide = 1 (Direita)
    // Se a distância for mínima (t = 0), CameraSide = 0.5f (Centro) ou menos
    thirdPersonFollow.CameraSide = Mathf.Lerp(0.35f, 1f, t);
}
}