using UnityEngine;

public class DamageReceiver : MonoBehaviour
{
    public GameObject damagePopupPrefab;
    public Transform popupSpawnPoint;

    [Tooltip("Tempo máximo (em segundos) entre os hits para os danos serem somados no mesmo popup")]
    public float combineTimeWindow = 0.1f;

    private DamagePopup lastPopup;
    private float lastDamageTime = -1f;

    public void TakeDamage(float damageAmount)
    {
        // Se existe um popup ativo e o tempo atual está dentro da janela permitida
        if (lastPopup != null && Time.time <= lastDamageTime + combineTimeWindow)
        {
            lastPopup.AddDamage(damageAmount);
        }
        else
        {
            // O tempo expirou ou é o primeiro dano: cria um novo popup
            Vector3 spawnPos = popupSpawnPoint != null ? popupSpawnPoint.position : transform.position;
            GameObject popupObj = Instantiate(damagePopupPrefab, spawnPos, Quaternion.identity);

            lastPopup = popupObj.GetComponent<DamagePopup>();
            lastPopup.Setup(damageAmount);
        }

        // Atualiza o tempo do último dano recebido
        lastDamageTime = Time.time;
    }
}