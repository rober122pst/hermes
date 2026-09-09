using System.Collections.Generic;
using UnityEngine;

public class ShipManager : MonoBehaviour
{
    public static ShipManager Instance { get; private set; }
    // Lista com todas as naves ativas no jogo
    public List<Transform> activeShips = new List<Transform>();

    // Raio de colisao das naves (ajuste conforme o tamanho do seu modelo 3D)
    public float shipCollisionRadius = 1.5f;

    private SpatialGrid spatialGrid;

    [Header("Configurações de Enxame")]
    public float maxSeparationSpeedPerFrame = 2f;

    private void Start()
    {
        spatialGrid = new SpatialGrid();
        Instance = this;
    }

    private void Update()
    {
        // PASSO 1: Limpa o grid do frame anterior
        spatialGrid.ClearGrid();

        // PASSO 2: Registra a nova posicao de todas as naves
        foreach (Transform ship in activeShips)
        {
            spatialGrid.RegisterShip(ship);
        }

        // PASSO 3: Verifica as colisoes usando o particionamento espacial
        foreach (Transform currentShip in activeShips)
        {
            // Descobre em qual cubo/setor a nave atual esta
            Vector3Int currentSector = spatialGrid.GetSpatialSector(currentShip.position);

            // Pega os 27 cubos ao redor dela (incluindo o dela mesma)
            List<Vector3Int> surroundingSectors = spatialGrid.GetSurroundingSectors(currentSector);

            // Pega apenas as naves que estao nesses 27 cubos
            List<Transform> nearbyShips = spatialGrid.GetShipsInSectors(surroundingSectors);

            // Compara a nave atual apenas com essa pequena lista de vizinhas
            foreach (Transform otherShip in nearbyShips)
            {
                // Ignora a checagem se for ela mesma
                if (currentShip == otherShip) continue;

                // A soma dos raios das duas naves
                float combinedRadius = shipCollisionRadius * 2f;

                // A magica acontece aqui: a checagem matematica
                if (spatialGrid.CheckShipCollision(currentShip.position, otherShip.position, combinedRadius))
                {
                    HandleCollisionSmooth(currentShip, otherShip, combinedRadius);
                }
            }
        }
    }

    private void HandleCollisionSmooth(Transform shipA, Transform shipB, float combinedRadius)
    {
        // 1. Calcula a direção e a distância atual entre as duas naves
        Vector3 separationDirection = shipA.position - shipB.position;
        float currentDistance = separationDirection.magnitude;

        // Evita divisão por zero se nascerem exatamente no mesmo ponto
        if (currentDistance == 0f)
        {
            separationDirection = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));
            currentDistance = 0.01f;
        }

        // 2. Normaliza a direção (transforma o comprimento em 1)
        separationDirection /= currentDistance;

        // 3. Descobre o quão "dentro" uma da outra elas estão (overlap)
        float overlap = combinedRadius - currentDistance;

        // 4. Calcula o vetor de empurrão cru (sem suavização)
        // Dividimos por 2 pois ambas as naves vão se mover.
        Vector3 rawPushVector = separationDirection * (overlap * 0.5f);

        // ---------------------------------------------------------
        // A MÁGICA DA SUAVIZAÇÃO ACONTECE AQUI:
        // ---------------------------------------------------------

        // Em vez de aplicar o pushVector inteiro, nós limitamos o seu
        // tamanho máximo com base na nossa velocidade configurada e o tempo do frame.
        // Isso impede que naves que entraram muito fundo pulem instantaneamente para fora.

        float maxAllowedDisplacement = maxSeparationSpeedPerFrame * Time.deltaTime;

        Vector3 clampedPushVector = Vector3.ClampMagnitude(rawPushVector, maxAllowedDisplacement);

        // 5. Aplica o movimento limitado diretamente nas posições
        shipA.position += clampedPushVector;
        shipB.position -= clampedPushVector;
    }

    public void AddActiveShip(Transform ship)
    {
        if (!activeShips.Contains(ship))
        {
            activeShips.Add(ship);
        }
    }
}