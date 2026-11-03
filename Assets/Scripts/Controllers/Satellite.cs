using UnityEngine;

public class Satellite : MonoBehaviour, IInteractive
{
    [SerializeField] private GameObject prefab;
    private SphereCollider col;

    private void Start()
    {
        col = gameObject.AddComponent<SphereCollider>();
        col.radius = 3;
        col.isTrigger = true;
    }

    public void ShowDialogueBox()
    {
        GameObject dialogBox = Instantiate(prefab);
    }
}
