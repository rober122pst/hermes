using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Player.UI
{
    public class RayTractor : MonoBehaviour
    {
        [Header("Configurações Visuais")]
        public RectTransform cursorUI; // Arraste a Image do CustomCursor aqui
        public Sprite cursorNormal;
        public Sprite cursorInterativo;
        private Image cursorImage;

        [Header("Configurações do Ímã")]
        public LayerMask objetosInterativos; // Defina a layer dos objetos que atraem o cursor
        public float raioDeDeteccao = 1.5f; // Quão perto (em unidades do mundo) o mouse precisa chegar

        private Camera cam;
        private Vector3 cursorInitialPos;

        void Start()
        {
            cam = Camera.main;
            cursorImage = cursorUI.GetComponent<Image>();

            // Oculta o cursor original do sistema operacional
            Cursor.visible = false;

            // Opcional: Trava o cursor na tela do jogo para ele não escapar para outro monitor
            Cursor.lockState = CursorLockMode.Confined;

            cursorInitialPos = cursorUI.position;
        }

        void Update()
        {
            Vector2 posicaoMouse = Mouse.current.position.ReadValue();
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            // Dispara um raio grosso (esfera) para encontrar objetos próximos na layer especificada
            if (Physics.SphereCast(ray, raioDeDeteccao, out hit, 100f, objetosInterativos))
            {
                // Detectou o objeto: muda a arte e trava o UI cursor no centro dele
                cursorImage.sprite = cursorInterativo;

                // Converte a posição 3D do objeto para uma coordenada 2D na tela
                Vector3 posicaoNaTela = cam.WorldToScreenPoint(hit.transform.position);
                cursorUI.position = Vector3.Lerp(cursorUI.position, posicaoNaTela, 0.6f);
            }
            else
            {
                // Nada detectado: o cursor segue o mouse livremente e usa a arte padrão
                cursorUI.position = Vector3.Lerp(cursorUI.position, cursorInitialPos, 0.6f);
                cursorImage.sprite = cursorNormal;
            }
        }
    }
}
