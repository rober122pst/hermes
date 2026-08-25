using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    [RequireComponent(typeof(CharacterController))]
    public class NaveController : MonoBehaviour
    {
        [Header("Configurações de Velocidade")]
        public float velocidadeFrente = 15f;
        public float velocidadeRe = 5f;
        public float velocidadeRotacao = 100f;

        [Header("Configurações de Curva (Estilo Avião)")]
        public float anguloInclinacao = 45f; // Grau máximo que a nave deita (Roll)
        public float velocidadeInclinacao = 5f; // Quão rápido ela deita e volta ao eixo

        private CharacterController controller;
        private StarterAssetsInputs input;

        // Precisamos armazenar a rotação atual para controlar os eixos independentemente
        private float yaw; // Rotação Horizontal (Y)
        private float roll; // Inclinação (Z)

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<StarterAssetsInputs>();

            // Captura a rotação inicial que a nave já tem na cena
            yaw = transform.eulerAngles.y;
        }

        private void Update()
        {
            // 1. ROTAÇÃO (Estilo Avião)

            // Atualiza a direção do bico da nave (Eixo Y) com base no input (A / D)
            yaw += input.move.x * velocidadeRotacao * Time.deltaTime;

            // Calcula a inclinação alvo (Eixo Z). 
            // O valor é negativo para que, ao apertar D (input positivo), ela deite para a direita.
            float targetRoll = -input.move.x * anguloInclinacao;

            // Interpola a inclinação atual até a inclinação alvo de forma suave
            roll = Mathf.Lerp(roll, targetRoll, velocidadeInclinacao * Time.deltaTime);

            // Aplica as rotações direto no Transform.
            // (O Eixo X continua em 0 para a nave não empinar o bico para cima/baixo nesse código)
            transform.rotation = Quaternion.Euler(0, yaw, roll);

            // 2. MOVIMENTAÇÃO (Teclas W e S)
            float velocidadeAtual = input.move.y >= 0 ? velocidadeFrente : velocidadeRe;

            // Como a nave está inclinada no Z, o transform.forward continua apontando perfeitamente
            // para frente, sem fazer ela "afundar" ou "subir" indesejadamente.
            Vector3 direcao = transform.forward * input.move.y;

            // Move a nave
            controller.Move(direcao * velocidadeAtual * Time.deltaTime);
        }
    }
}