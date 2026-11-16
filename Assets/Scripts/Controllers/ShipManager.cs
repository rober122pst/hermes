using System.Collections.Generic;
using UnityEngine;

public class ShipManager : MonoBehaviour
{
    public static ShipManager Instance { get; private set; }
    public List<Transform> activeShips = new List<Transform>();

    [Min(0f)] public float shipCollisionRadius = 1.5f;
    [Min(0f)] public float maxSeparationSpeedPerFrame = 2f;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void LateUpdate()
    {
        RemoveDisabledShips();
        float combinedRadius = shipCollisionRadius * 2f;
        float maxPush = maxSeparationSpeedPerFrame * Time.deltaTime;

        for (int i = 0; i < activeShips.Count; i++)
        {
            Transform ship = activeShips[i];
            for (int j = i + 1; j < activeShips.Count; j++)
            {
                Transform other = activeShips[j];
                Vector3 offset = ship.position - other.position;
                float distanceSquared = offset.sqrMagnitude;
                if (distanceSquared >= combinedRadius * combinedRadius) continue;

                float distance = Mathf.Sqrt(distanceSquared);
                Vector3 direction = distance > 0.001f ? offset / distance : Vector3.right;
                float push = Mathf.Min((combinedRadius - distance) * 0.5f, maxPush);
                ship.position += direction * push;
                other.position -= direction * push;
            }
        }
    }

    public void AddActiveShip(Transform ship)
    {
        if (ship != null && !activeShips.Contains(ship)) activeShips.Add(ship);
    }

    public void RemoveActiveShip(Transform ship)
    {
        activeShips.Remove(ship);
    }

    public void RemoveDisabledShips()
    {
        for (int i = activeShips.Count - 1; i >= 0; i--)
        {
            Transform ship = activeShips[i];
            if (ship == null || !ship.gameObject.activeInHierarchy) activeShips.RemoveAt(i);
        }
    }
}
