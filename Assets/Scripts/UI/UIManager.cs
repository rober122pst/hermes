using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Telas")]
    [SerializeField]
    private GameObject levelUpScreen;

    private void Start()
    {
        Instance = this;
    }

    public void ShowLevelUpScreen()
    {
        levelUpScreen.SetActive(true);
    }
}
