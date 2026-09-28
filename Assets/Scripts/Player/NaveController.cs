using UnityEngine;
using System.Collections.Generic;

namespace Player
{
    public class NaveController : MonoBehaviour
    {
        [Header("Configurações de Movimento")]
        public float velocidade = 1f;
        public float velocidadeSprint = 5f;

        [Header("Configurações de Rotação (Espaço)")]
        [SerializeField]
        float multAnguloVelocidade = 0.5f;
        [SerializeField]
        float multVelocidadeRotacao = 0.05f;

        [Header("Configurações de Tiro")]
        [SerializeField]
        float tempoEntreTiros = 0.5f;
        float tempoUltimoTiro = 0f;
        [SerializeField]
        float danoDoTiro = 10f;
        [SerializeField]
        float distanciaDaMira = 500f;
        [SerializeField]
        float distanciaMinima = 20f;

        [Header("Audios")]
        [SerializeField]
        private AudioClip tiroAudioClip;

        private Rigidbody rb;
        private StarterAssetsInputs input;
        private Camera cam;
        private AudioSource audioSource;

        [Space(10)]
        [SerializeField]
        List<GameObject> weapons;
        public WeaponManager weaponManager;
        public WeaponData weaponData;

        private void Awake()
        {
            cam = Camera.main;
            rb = GetComponent<Rigidbody>();
            input = GetComponent<StarterAssetsInputs>();
            audioSource = GetComponent<AudioSource>();
            weaponManager.EquipWeapon(weaponData);
        }

        // private void Update()
        // {
        //     if (input.fire && Time.time > tempoUltimoTiro)
        //     {
        //         Atirar();
        //     }
        // }

        private void FixedUpdate()
        {
            float velocidadeAlvo = input.sprint ? velocidadeSprint : velocidade;

            rb.AddForce(rb.transform.TransformDirection(Vector3.forward) * input.move.y * velocidadeAlvo, ForceMode.VelocityChange);
            rb.AddForce(rb.transform.TransformDirection(Vector3.right) * input.move.x * velocidadeAlvo, ForceMode.VelocityChange);

            rb.AddTorque(rb.transform.right * multAnguloVelocidade * input.look.y * -1, ForceMode.VelocityChange);
            rb.AddTorque(rb.transform.up * multAnguloVelocidade * input.look.x, ForceMode.VelocityChange);

            rb.AddTorque(rb.transform.forward * multVelocidadeRotacao * -input.roll.x, ForceMode.VelocityChange);
        }

        Vector3 PosInScreen(Vector3 posWorld)
        {
            Vector3 posScreen = cam.WorldToScreenPoint(posWorld);

            if (cam.targetTexture != null)
            {
                posScreen.x *= (float)Screen.width / cam.targetTexture.width;
                posScreen.y *= (float)Screen.height / cam.targetTexture.height;
            }

            return posScreen;
        }
    }
}