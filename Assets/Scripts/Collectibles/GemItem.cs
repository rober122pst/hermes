public class GemItem : Collectible
{
    public override void Collected()
    {
        if (!isActiveAndEnabled || player == null || !player.IsAlive) return;
        player.CollectGem();
        base.Collected();
    }
}
