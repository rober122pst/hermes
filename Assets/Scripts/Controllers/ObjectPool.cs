using UnityEngine;
using System.Collections.Generic;


public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    public List<PoolID> pools;
    [Tooltip("Objeto pai para os objetos instanciados")]
    public Transform parent;

    private Dictionary<PoolID, List<GameObject>> instances;
    private Dictionary<PoolID, int> lastIndexes;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        instances = new Dictionary<PoolID, List<GameObject>>();
        lastIndexes = new Dictionary<PoolID, int>();
        foreach (PoolID data in pools)
        {
            List<GameObject> pool = new List<GameObject>();
            for (int i = 0; i < data.amount; i++)
            {
                GameObject obj = Instantiate(data.prefab, parent);
                obj.SetActive(false);
                pool.Add(obj);
            }

            instances.Add(data, pool);
            lastIndexes.Add(data, 0);
        }
    }

    public GameObject GetInstance(PoolID id)
    {
        if (!instances.TryGetValue(id, out List<GameObject> pool))
        {
            Debug.LogError($"Pool com ID {id} não existe.");
            return null;
        }

        int index = lastIndexes[id];

        GameObject obj = pool[index];

        lastIndexes[id] = (index + 1) % pool.Count;

        return obj;
    }
}
