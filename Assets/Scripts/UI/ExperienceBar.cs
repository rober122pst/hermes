using Player;
using UnityEngine;
using UnityEngine.UI;

public class ExperienceBar : MonoBehaviour
{
    [SerializeField]
    private NaveController player;
    [SerializeField]
    private Slider xpBar;

    private void OnEnable()
    {
        if (player == null || xpBar == null) return;
        player.stats.OnExperienceChanged += UpdateXpBar;
        UpdateXpBar(player.stats.Experience);
    }

    private void OnDisable()
    {
        if (player == null) return;
        player.stats.OnExperienceChanged -= UpdateXpBar;
    }

    private void UpdateXpBar(float xp)
    {
        xpBar.value = xp / player.stats.MaxExperience;
    }
}
