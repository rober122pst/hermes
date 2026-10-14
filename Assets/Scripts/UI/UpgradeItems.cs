using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeItems : MonoBehaviour
{
    public List<WeaponData> weapons;
    public List<Button> buttons;

    private List<int> sortedWeapons;

    void OnEnable()
    {
        int index = Random.Range(0, weapons.Count);

    }

    // Update is called once per frame
    void Update()
    {

    }
}
