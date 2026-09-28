using UnityEngine;

[RequireComponent(typeof(ShipManager))]
public class ShipGizmoDebugger : MonoBehaviour
{
    [Header("Configurações Visuais dos Gizmos")]
    public bool showCollisionRadius = true;
    public Color safeColor = Color.cyan;
    public Color overlapColor = Color.red;

    private void OnDrawGizmos()
    {
        // Garante que a instância e a lista de naves ativas existem antes de desenhar[cite: 1]
        if (ShipManager.Instance == null || ShipManager.Instance.activeShips == null)
            return;

        // Itera sobre a lista com todas as naves ativas no jogo[cite: 1]
        foreach (Transform ship in ShipManager.Instance.activeShips)
        {
            if (ship == null) continue;

            float currentRadius = ShipManager.Instance.shipCollisionRadius; // Puxa o raio de colisão do modelo 3D configurado no manager[cite: 1]
            Gizmos.color = safeColor;

            // Verifica visualmente se há sobreposição (overlap) para alterar a cor do Gizmo
            if (IsOverlapping(ship, currentRadius))
            {
                Gizmos.color = overlapColor;
            }

            // Desenha a esfera no modo Scene da Unity
            if (showCollisionRadius)
            {
                Gizmos.DrawWireSphere(ship.position, currentRadius);
            }
        }
    }

    private bool IsOverlapping(Transform currentShip, float radius)
    {
        // Usa a mesma lógica base para comparar a nave atual com as outras[cite: 1]
        foreach (Transform otherShip in ShipManager.Instance.activeShips)
        {
            // Ignora a checagem se for ela mesma[cite: 1]
            if (currentShip == otherShip || otherShip == null) continue;

            float distance = Vector3.Distance(currentShip.position, otherShip.position);
            float combinedRadius = radius * 2f; // A soma dos raios das duas naves[cite: 1]

            // Se a distância for menor que a soma dos raios, desenha uma linha indicando a direção da separação[cite: 1]
            if (distance < combinedRadius)
            {
                Gizmos.DrawLine(currentShip.position, otherShip.position);
                return true;
            }
        }
        return false;
    }
}