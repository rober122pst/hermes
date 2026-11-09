using UnityEngine;

public class Satellite : MonoBehaviour, IInteractive
{
    [SerializeField] private GameObject prefab;
    private GameObject dialogBoxInstance;

    public void ShowDialogueBox()
    {
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
        if (dialogBoxInstance != null)
        {
            dialogBoxInstance.GetComponent<PromptOrbit>().Hide();
        }
    }

    private void OnDisable()
    {
        HideDialogueBox();
    }
}
