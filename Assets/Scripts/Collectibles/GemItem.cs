using UnityEngine;

public class GemItem : Collectible
{
    public override void Collected()
    {
        base.Collected();
        player.stats.Experience += player.stats.IncrementExperience;
    }
}
