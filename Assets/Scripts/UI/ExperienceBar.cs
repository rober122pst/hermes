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
        player.stats.OnExperienceChanged += UpdateXpBar;
    }

    private void OnDisable()
    {
        player.stats.OnExperienceChanged -= UpdateXpBar;
    }

    private void UpdateXpBar(float xp)
    {
        xpBar.value = xp / player.stats.MaxExperience;
    }
}
