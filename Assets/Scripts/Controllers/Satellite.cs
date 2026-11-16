using System.Collections;
using Player;
using UnityEngine;

public class Satellite : MonoBehaviour, IInteractive
{
    [SerializeField] private GameObject prefab;

    private GameObject dialogBoxInstance;
    private GameObject panelInstance;
    private CanvasGroup panelCanvasGroup;
    private RectTransform panelWindow;
    private Coroutine panelTransition;
    private UIManager pauseManager;
    private StarterAssetsInputs playerInput;
    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisibility;
    private bool previousLookInputState;
    private bool isFocused;
    private bool isPanelOpen;

    public void ShowDialogueBox()
    {
        isFocused = true;

        if (prefab == null || (panelInstance != null && panelInstance.activeSelf))
        {
            return;
        }

        if (dialogBoxInstance != null)
        {
            dialogBoxInstance.GetComponent<PromptOrbit>().Show();
            return;
        }

        dialogBoxInstance = Instantiate(prefab);
        dialogBoxInstance.transform.position = transform.position;
        dialogBoxInstance.GetComponent<PromptOrbit>().Setup(transform);
        dialogBoxInstance.GetComponent<PromptOrbit>().Show();
    }

    public void HideDialogueBox()
    {
        isFocused = false;
        HidePrompt();
    }

    private void HidePrompt()
    {
        if (dialogBoxInstance != null)
        {
            dialogBoxInstance.GetComponent<PromptOrbit>().Hide();
        }
    }

    public void OpenPanel(StarterAssetsInputs input)
    {
        if (!isFocused || isPanelOpen)
        {
            return;
        }

        if (panelInstance == null)
        {
            Canvas canvas = FindScreenCanvas();
            if (canvas == null)
            {
                Debug.LogWarning("Nenhum Canvas de tela foi encontrado para o painel do satélite.");
                return;
            }

            panelInstance = SatellitePanelTemplate.Create(canvas, ClosePanel);
            panelCanvasGroup = panelInstance.GetComponent<CanvasGroup>();
            panelWindow = panelInstance.transform.Find("Window") as RectTransform;
        }

        pauseManager = UIManager.Instance;
        if (pauseManager != null)
        {
            if (!pauseManager.TryBeginTacticalPause(this, input)) return;
        }
        else
        {
            playerInput = input;
            previousCursorLockMode = Cursor.lockState;
            previousCursorVisibility = Cursor.visible;
            if (playerInput != null)
            {
                previousLookInputState = playerInput.cursorInputForLook;
                playerInput.cursorInputForLook = false;
                playerInput.look = Vector2.zero;
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        isPanelOpen = true;
        panelInstance.SetActive(true);
        HidePrompt();
        StartPanelTransition(true);
    }

    private void ClosePanel()
    {
        if (!isPanelOpen)
        {
            return;
        }

        isPanelOpen = false;
        if (pauseManager != null)
        {
            pauseManager.EndTacticalPause(this);
            pauseManager = null;
        }
        else
        {
            Cursor.lockState = previousCursorLockMode;
            Cursor.visible = previousCursorVisibility;
            if (playerInput != null)
            {
                playerInput.cursorInputForLook = previousLookInputState;
                playerInput.look = Vector2.zero;
                playerInput = null;
            }
        }

        if (isActiveAndEnabled) StartPanelTransition(false);
    }

    private void StartPanelTransition(bool opening)
    {
        if (panelTransition != null)
        {
            StopCoroutine(panelTransition);
        }

        panelTransition = StartCoroutine(AnimatePanel(opening));
    }

    private IEnumerator AnimatePanel(bool opening)
    {
        const float duration = 0.2f;
        float startAlpha = panelCanvasGroup.alpha;
        Vector3 startScale = panelWindow.localScale;
        float targetAlpha = opening ? 1f : 0f;
        Vector3 targetScale = Vector3.one * (opening ? 1f : 0.92f);
        float elapsed = 0f;

        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = progress * progress * (3f - 2f * progress);
            panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, easedProgress);
            panelWindow.localScale = Vector3.Lerp(startScale, targetScale, easedProgress);
            yield return null;
        }

        panelCanvasGroup.alpha = targetAlpha;
        panelWindow.localScale = targetScale;
        panelCanvasGroup.interactable = opening;
        panelCanvasGroup.blocksRaycasts = opening;
        panelTransition = null;

        if (!opening)
        {
            panelInstance.SetActive(false);
            if (isFocused)
            {
                ShowDialogueBox();
            }
        }
    }

    private static Canvas FindScreenCanvas()
    {
        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace)
            {
                return canvas;
            }
        }

        return null;
    }

    private void OnDisable()
    {
        ClosePanel();
        if (panelTransition != null)
        {
            StopCoroutine(panelTransition);
            panelTransition = null;
        }

        if (panelInstance != null)
        {
            panelInstance.SetActive(false);
        }

        HideDialogueBox();
    }

    private void OnDestroy()
    {
        if (panelInstance != null)
        {
            Destroy(panelInstance);
        }
    }

}
