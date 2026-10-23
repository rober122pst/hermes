using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Player;

public class UpgradeItems : MonoBehaviour
{
    public BuyItemButton[] buttons;
    public StarterAssetsInputs inputs;

    private List<WeaponData> randomWeapons;

    void Reset()
    {
        buttons = GetComponentsInChildren<BuyItemButton>();
    }

    void OnEnable()
    {
        // 1. Pausa o jogo e altera os estados do cursor apenas UMA vez ao abrir a interface
        Time.timeScale = 0f;
        Cursor.visible = true;
        inputs.SetCursorState(false);

        // 2. Desativa o movimento da câmera
        inputs.cursorInputForLook = false;

        inputs.look = Vector2.zero;

        randomWeapons = WeaponManager.Instance.GetRandomWeapons();

        CleanListeners();

        for (int i = 0; i <= buttons.Length - 1; i++)
        {
            if (i >= randomWeapons.Count)
            {
                buttons[i].gameObject.SetActive(false);
                break;
            }
            buttons[i].gameObject.SetActive(true);
            string weaponName = randomWeapons[i].weaponName;
            int level = WeaponManager.Instance.GetLevelByName(weaponName);
            string description = level > 0 ? randomWeapons[i].updates.weaponLevels[level - 1].description : "";
            UnityAction action = BuyItem(randomWeapons[i]);
            buttons[i].Setup(weaponName, description, level + 1, action);
        }
    }

    void CleanListeners()
    {
        for (int i = 0; i <= buttons.Length - 1; i++)
        {
            buttons[i].RemoveListener();
        }
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
        // 3. Restaura as configurações ao fechar a interface
        Time.timeScale = 1f;
        inputs.SetCursorState(true);
        Cursor.visible = false;
        inputs.look = Vector2.zero;

        // 4. Reativa o movimento da câmera
        inputs.cursorInputForLook = true;
    }
}
