using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class NaveController : MonoBehaviour
    {
        [Header("Configurações de Movimento")]
        public float velocidade = 1f;

        [Header("Configurações de Rotação (Espaço)")]
        [SerializeField]
        float multAnguloVelocidade = 0.5f;
        [SerializeField]
        float multVelocidadeRotacao = 0.05f;

        private Rigidbody rigidbody;
        private StarterAssetsInputs input;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Awake()
        {
            rigidbody = GetComponent<Rigidbody>();
            input = GetComponent<StarterAssetsInputs>();
        }

        private void FixedUpdate()
        {
            rigidbody.AddForce(rigidbody.transform.TransformDirection(Vector3.forward) * input.move.y * velocidade, ForceMode.VelocityChange);
            rigidbody.AddForce(rigidbody.transform.TransformDirection(Vector3.right) * input.move.x * velocidade, ForceMode.VelocityChange);

            rigidbody.AddTorque(rigidbody.transform.right * multAnguloVelocidade * input.look.y * -1, ForceMode.VelocityChange);
            rigidbody.AddTorque(rigidbody.transform.up * multAnguloVelocidade * input.look.x, ForceMode.VelocityChange);

            rigidbody.AddTorque(rigidbody.transform.forward * multVelocidadeRotacao * input.roll.x, ForceMode.VelocityChange);
        }
    }
}