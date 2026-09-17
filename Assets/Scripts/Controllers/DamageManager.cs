using UnityEngine;

public class DamageManager : MonoBehaviour
{
    public static DamageManager Instance { get; private set; }
    [SerializeField] private ObjectPool damagePool;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public GameObject ShowDamage(float amount, Vector3 position)
    {
        position += new Vector3(Random.Range(-1f, 1f), Random.Range(0.5f, 2.5f), 0); // Adiciona um deslocamento aleatório para o popup
        GameObject damagePopup = damagePool.GetInstance();
        damagePopup.transform.position = position;
        damagePopup.GetComponent<DamagePopup>().Setup(amount);
        damagePopup.SetActive(true);

        return damagePopup;
    }
}
