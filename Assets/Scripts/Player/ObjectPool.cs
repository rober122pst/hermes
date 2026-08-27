using UnityEngine;
using System.Collections.Generic;

public class ObjectPool : MonoBehaviour
{
    [Tooltip("Objeto para ser instanciado")]
    public GameObject prefab;
    [Tooltip("Quantidade de objetos a serem instanciados")]
    public int amount = 5;
    [Tooltip("Objeto pai para os objetos instanciados")]
    public Transform parent;

    List<GameObject> instances;
    int lastIndex = 0;

    void Awake()
    {
        instances = new List<GameObject>();
        for (int i = 0; i < amount; i++)
        {
            GameObject obj = Instantiate(prefab, parent);
            obj.SetActive(false);
            instances.Add(obj);
        }
    }

    public GameObject GetInstance()
    {
        GameObject obj = instances[lastIndex];
        lastIndex = (lastIndex + 1) % instances.Count;
        return obj;
    }
}
