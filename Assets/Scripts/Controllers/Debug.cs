using UnityEngine;
using TMPro;

namespace GameDebug
{

    public class Debug : MonoBehaviour
    {
        public TextMeshProUGUI textMeshPro;
        float fps;

        // Update is called once per frame
        void Update()
        {
            fps = 1.0f / Time.deltaTime;

            textMeshPro.text = "FPS atual: " + Mathf.Ceil(fps);

            if (fps < 30)
            {
                textMeshPro.color = Color.red;
            }
            else if (fps < 60)
            {
                textMeshPro.color = Color.yellow;
            }
            else
            {
                textMeshPro.color = Color.green;
            }


        }
    }

}