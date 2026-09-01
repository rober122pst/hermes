using UnityEngine;

[ExecuteAlways]
public class FollowCameraPosition : MonoBehaviour
{
    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            // Mantém a luz sempre centrada na câmera para o teste de oclusão não falhar por distância
            transform.position = cam.transform.position;
        }
    }
}