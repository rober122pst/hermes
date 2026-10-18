using Player;
using UnityEngine;
using UnityEngine.UI;

public class ExperienceBar : MonoBehaviour
{
    [SerializeField]
    private NaveController player;
    [SerializeField]
    private Slider xpBar;

    private void Update()
    {
        xpBar.value = player.stats.Experience / player.stats.MaxExperience;
    }
}
