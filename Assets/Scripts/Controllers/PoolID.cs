using UnityEngine;

[CreateAssetMenu(fileName = "PoolID", menuName = "Game/PoolID")]
public class PoolID : ScriptableObject
{
    public GameObject prefab;
    public int amount = 5;
}