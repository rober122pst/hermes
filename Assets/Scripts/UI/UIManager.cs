using Player;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Telas")]
    [SerializeField] private GameObject levelUpScreen;

    private object pauseOwner;
    private StarterAssetsInputs pausedInput;
    private CursorLockMode savedLockMode;
    private bool savedCursorVisibility;
    private bool savedLookInput;
    private float impactEndTime;

    public GameObject LevelUpScreen => levelUpScreen;
    public bool IsTacticallyPaused => pauseOwner != null;
    public bool OwnsTacticalPause(object owner) => owner != null && ReferenceEquals(pauseOwner, owner);

    private void Awake()
    {
        Instance = this;
    }

    public bool TryShowLevelUp(StarterAssetsInputs input)
    {
        if (levelUpScreen == null || levelUpScreen.activeSelf || IsTacticallyPaused ||
            WeaponManager.Instance == null || !WeaponManager.Instance.HasUpgradeableWeapons()) return false;

        if (!TryBeginTacticalPause(levelUpScreen, input)) return false;
        levelUpScreen.SetActive(true);
        return true;
    }

    public bool TryBeginTacticalPause(object owner, StarterAssetsInputs playerInput = null)
    {
        if (owner == null || IsTacticallyPaused) return false;

        pauseOwner = owner;
        pausedInput = playerInput;
        savedLockMode = Cursor.lockState;
        savedCursorVisibility = Cursor.visible;
        if (pausedInput != null)
        {
            savedLookInput = pausedInput.cursorInputForLook;
            pausedInput.cursorInputForLook = false;
            pausedInput.look = Vector2.zero;
        }

        impactEndTime = 0f;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        return true;
    }

    public void EndTacticalPause(object owner)
    {
        if (!OwnsTacticalPause(owner)) return;

        pauseOwner = null;
        Time.timeScale = 1f;
        Cursor.lockState = savedLockMode;
        Cursor.visible = savedCursorVisibility;
        if (pausedInput != null)
        {
            pausedInput.cursorInputForLook = savedLookInput;
            pausedInput.look = Vector2.zero;
            pausedInput = null;
        }
    }

    public void PulseImpact()
    {
        if (IsTacticallyPaused || impactEndTime > 0f || Time.timeScale <= 0f) return;
        impactEndTime = Time.unscaledTime + 0.055f;
        Time.timeScale = 0.05f;
    }

    private void Update()
    {
        if (impactEndTime <= 0f || Time.unscaledTime < impactEndTime) return;
        impactEndTime = 0f;
        if (!IsTacticallyPaused) Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        if (IsTacticallyPaused) EndTacticalPause(pauseOwner);
        if (impactEndTime > 0f) Time.timeScale = 1f;
        Instance = null;
    }
}
