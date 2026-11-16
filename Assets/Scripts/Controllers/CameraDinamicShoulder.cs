using UnityEngine;
using Unity.Cinemachine;

namespace Game.Controllers
{
    public class CameraDinamicShoulder : MonoBehaviour
    {
        [Header("Cinemachine References")]
        public CinemachineCamera vcam;
        private CinemachineThirdPersonFollow thirdPersonFollow;

        [Header("Distance Settings")]
        public float maxDistance = 2.2f;
        public float minDistance = 0.5f;

        private void Start()
        {
            if (vcam != null)
            {
                thirdPersonFollow = vcam.GetComponent<CinemachineThirdPersonFollow>();
            }
        }

        private void Update()
        {
            if (thirdPersonFollow == null || vcam == null || vcam.Follow == null || Camera.main == null)
            {
                return;
            }

            // O Cinemachine ajusta a posição da câmera conforme as colisões.
            // Mede a distância entre o alvo de acompanhamento e a câmera principal.
            float currentDistance = Vector3.Distance(Camera.main.transform.position, vcam.Follow.position);

            // Converte a distância em um valor entre zero e um.
            float distanceFactor = Mathf.InverseLerp(minDistance, maxDistance, currentDistance);

            // Ajusta o deslocamento lateral conforme a distância da câmera.
            // No alcance máximo, a câmera fica à direita; no mínimo, aproxima-se do centro.
            thirdPersonFollow.CameraSide = Mathf.Lerp(0.35f, 1f, distanceFactor);
        }
    }
}
