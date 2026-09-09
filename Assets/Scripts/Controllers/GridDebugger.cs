using UnityEngine;

// Coloque este componente num GameObject vazio na cena
public class GridDebugger : MonoBehaviour
{
    // Tamanho do cubo/setor base no espaco 3D
    public float sectorSize = 10f;

    // Referencia ao seu jogador ou uma nave de teste para saber o setor atual
    public Transform playerShip;

    private void OnDrawGizmos()
    {
        // 1. Opcional: Desenha o grid inteiro como linhas brancas finas
        // ATENCAO: Em mapas gigantes, isso pode pesar a scene view, 
        // limite o numero de linhas desenhadas.
        Gizmos.color = new Color(1, 1, 1, 0.2f); // Branco com transparência
        int gridSizeCount = 10; // Número de linhas para cada lado do centro (0,0,0)

        for (int x = -gridSizeCount; x <= gridSizeCount; x++)
        {
            for (int y = -gridSizeCount; y <= gridSizeCount; y++)
            {
                Gizmos.DrawLine(
                    new Vector3(x * sectorSize, y * sectorSize, -gridSizeCount * sectorSize),
                    new Vector3(x * sectorSize, y * sectorSize, gridSizeCount * sectorSize));

                Gizmos.DrawLine(
                    new Vector3(x * sectorSize, -gridSizeCount * sectorSize, y * sectorSize),
                    new Vector3(x * sectorSize, gridSizeCount * sectorSize, y * sectorSize));

                Gizmos.DrawLine(
                    new Vector3(-gridSizeCount * sectorSize, x * sectorSize, y * sectorSize),
                    new Vector3(gridSizeCount * sectorSize, x * sectorSize, y * sectorSize));
            }
        }

        // 2. Fundamental: Desenha os 27 setores vizinhos ativos do jogador
        if (playerShip != null)
        {
            // Descobre o setor atual do jogador usando a matematica do sistema
            int currentSectorX = Mathf.FloorToInt(playerShip.position.x / sectorSize);
            int currentSectorY = Mathf.FloorToInt(playerShip.position.y / sectorSize);
            int currentSectorZ = Mathf.FloorToInt(playerShip.position.z / sectorSize);

            // Desenha o setor onde o jogador ESTA com uma cor diferente
            Gizmos.color = new Color(0, 1, 0, 0.4f); // Verde transparente
            Vector3 centralPos = new Vector3(
                currentSectorX * sectorSize + (sectorSize / 2f),
                currentSectorY * sectorSize + (sectorSize / 2f),
                currentSectorZ * sectorSize + (sectorSize / 2f)
            );
            Gizmos.DrawCube(centralPos, Vector3.one * sectorSize);

            // Desenha os 26 setores vizinhos ativos que serao checados [00:02:46]
            Gizmos.color = new Color(1, 0, 0, 0.3f); // Vermelho transparente
            for (int xOffset = -1; xOffset <= 1; xOffset++)
            {
                for (int yOffset = -1; yOffset <= 1; yOffset++)
                {
                    for (int zOffset = -1; zOffset <= 1; zOffset++)
                    {
                        // Pula o setor central (onde o jogador esta)
                        if (xOffset == 0 && yOffset == 0 && zOffset == 0) continue;

                        Vector3 sectorPos = new Vector3(
                            (currentSectorX + xOffset) * sectorSize + (sectorSize / 2f),
                            (currentSectorY + yOffset) * sectorSize + (sectorSize / 2f),
                            (currentSectorZ + zOffset) * sectorSize + (sectorSize / 2f)
                        );

                        Gizmos.DrawCube(sectorPos, Vector3.one * sectorSize);
                    }
                }
            }
        }
    }
}