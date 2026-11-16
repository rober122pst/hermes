using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    private sealed class Pool
    {
        public readonly List<GameObject> Instances = new List<GameObject>();
        public int NextIndex;
    }

    public static ObjectPool Instance { get; private set; }

    public List<PoolID> pools;
    [Tooltip("Objeto pai para os objetos instanciados")]
    public Transform parent;

    private readonly Dictionary<PoolID, Pool> instances = new Dictionary<PoolID, Pool>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        foreach (PoolID id in pools)
        {
            if (id == null || id.prefab == null || instances.ContainsKey(id)) continue;

            Pool pool = new Pool();
            instances.Add(id, pool);
            for (int i = 0; i < id.amount; i++)
                pool.Instances.Add(CreateInactiveInstance(id));
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public GameObject GetInstance(PoolID id)
    {
        if (id == null || !instances.TryGetValue(id, out Pool pool))
        {
            Debug.LogError($"Pool com ID {id} não existe.");
            return null;
        }

        for (int i = 0; i < pool.Instances.Count; i++)
        {
            int index = (pool.NextIndex + i) % pool.Instances.Count;
            GameObject item = pool.Instances[index];
            if (item != null && item.activeSelf) continue;

            if (item == null)
            {
                item = CreateInactiveInstance(id);
                pool.Instances[index] = item;
            }

            pool.NextIndex = (index + 1) % pool.Instances.Count;
            return item;
        }

        GameObject extra = CreateInactiveInstance(id);
        pool.Instances.Add(extra);
        return extra;
    }

    private GameObject CreateInactiveInstance(PoolID id)
    {
        GameObject item = Instantiate(id.prefab, parent);
        item.SetActive(false);
        return item;
    }
}
