using System.Collections.Generic;
using UnityEngine;

public class SpatialGrid
{
    // Dicionario mapeando cada setor 3D para as naves contidas nele
    private Dictionary<Vector3Int, List<Transform>> spaceGrid = new Dictionary<Vector3Int, List<Transform>>();
    private float sectorSize = 10f;

    // 1. Limpa todas as listas de naves dos setores
    public void ClearGrid()
    {
        foreach (var sector in spaceGrid.Values)
        {
            sector.Clear();
        }
    }

    // 2. Registra uma nave no setor correspondente a sua posicao atual
    public void RegisterShip(Transform shipTransform)
    {
        Vector3Int sector = GetSpatialSector(shipTransform.position);

        // Se o setor ainda nao existe no dicionario, cria um novo
        if (!spaceGrid.ContainsKey(sector))
        {
            spaceGrid[sector] = new List<Transform>();
        }

        spaceGrid[sector].Add(shipTransform);
    }

    // 3. Coleta e retorna todas as naves que estao dentro de uma lista de setores
    public List<Transform> GetShipsInSectors(List<Vector3Int> sectors)
    {
        List<Transform> nearbyShips = new List<Transform>();
        foreach (Vector3Int sector in sectors)
        {
            if (spaceGrid.ContainsKey(sector) && spaceGrid[sector].Count > 0)
            {
                nearbyShips.AddRange(spaceGrid[sector]);
            }
        }
        return nearbyShips;
    }

    // Funcoes originais mantidas: GetSpatialSector, GetSurroundingSectors, CheckShipCollision...

    public Vector3Int GetSpatialSector(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / sectorSize);
        int y = Mathf.FloorToInt(position.y / sectorSize);
        int z = Mathf.FloorToInt(position.z / sectorSize);
        return new Vector3Int(x, y, z);
    }

    public List<Vector3Int> GetSurroundingSectors(Vector3Int currentSector)
    {
        List<Vector3Int> surroundingSectors = new List<Vector3Int>(27);
        for (int xOffset = -1; xOffset <= 1; xOffset++)
        {
            for (int yOffset = -1; yOffset <= 1; yOffset++)
            {
                for (int zOffset = -1; zOffset <= 1; zOffset++)
                {
                    surroundingSectors.Add(new Vector3Int(
                        currentSector.x + xOffset,
                        currentSector.y + yOffset,
                        currentSector.z + zOffset
                    ));
                }
            }
        }
        return surroundingSectors;
    }

    public bool CheckShipCollision(Vector3 shipA, Vector3 shipB, float combinedRadius)
    {
        float distanceSquared = (shipA - shipB).sqrMagnitude;
        return distanceSquared <= (combinedRadius * combinedRadius);
    }
}
