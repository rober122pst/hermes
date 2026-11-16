using UnityEngine;
using TMPro;

namespace Game.DebugTools
{
    public class FPSDebugger : MonoBehaviour
    {
        public TextMeshProUGUI fpsText;
        private float fps;

        void Update()
        {
            fps = 1.0f / Time.deltaTime;

            if (fpsText != null)
            {
                fpsText.text = "Current FPS: " + Mathf.Ceil(fps);

                if (fps < 30)
                {
                    fpsText.color = Color.red;
                }
                else if (fps < 60)
                {
                    fpsText.color = Color.yellow;
                }
                else
                {
                    fpsText.color = Color.green;
                }
            }
        }
    }
}