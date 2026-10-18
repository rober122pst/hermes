using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using Player;

public class UpgradeItems : MonoBehaviour
{
    public List<WeaponData> weapons;
    public List<Button> buttons;
    public StarterAssetsInputs inputs;

    private List<WeaponData> sortedWeapons;

    void OnEnable()
    {
        inputs.cursorLocked = false;
        inputs.cursorInputForLook = false;

        sortedWeapons = new List<WeaponData>(weapons);
        while (sortedWeapons.Count > buttons.Count)
        {
            sortedWeapons.RemoveAt(Random.Range(0, sortedWeapons.Count));
        }

        CleanListeners();

        for (int i = 0; i <= buttons.Count - 1; i++)
        {
            TextMeshProUGUI bntText = buttons[i].GetComponentInChildren<TextMeshProUGUI>();
            bntText.text = sortedWeapons[i].weaponName;
            buttons[i].onClick.AddListener(BuyItem(sortedWeapons[i]));
        }
    }

    void CleanListeners()
    {
        for (int i = 0; i <= buttons.Count - 1; i++)
        {
            buttons[i].onClick.RemoveAllListeners();
        }
    }

    // Update is called once per frame
    void Update()
    {
        Time.timeScale = 0f;
        Cursor.visible = true;
    }

    public UnityAction BuyItem(WeaponData item)
    {
        return () =>
        {
            gameObject.SetActive(false);
            WeaponManager.Instance.EquipWeapon(item);
        };
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
        inputs.cursorLocked = true;
        inputs.cursorInputForLook = true;
        Cursor.visible = false;
    }
}
