using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using Player;

public class UpgradeItems : MonoBehaviour
{
    public List<Button> buttons;
    public StarterAssetsInputs inputs;

    private List<WeaponData> randomWeapons;

    void OnEnable()
    {
        inputs.cursorLocked = false;
        inputs.cursorInputForLook = false;

        randomWeapons = WeaponManager.Instance.GetRandomWeapons();

        CleanListeners();

        for (int i = 0; i <= buttons.Count - 1; i++)
        {
            TextMeshProUGUI bntText = buttons[i].GetComponentInChildren<TextMeshProUGUI>();
            bntText.text = randomWeapons[i].weaponName;
            buttons[i].onClick.AddListener(BuyItem(randomWeapons[i]));
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
        Cursor.lockState = CursorLockMode.None;
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
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
