using UnityEngine;

public class EnemyBullet : MonoBehaviour, IDamageable
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private Renderer bulletRenderer;
    private MaterialPropertyBlock properties;

    private void Awake()
    {
        bulletRenderer = GetComponentInChildren<Renderer>(true);
        properties = new MaterialPropertyBlock();
    }

    public void SetAppearance(Color color, float size)
    {
        transform.localScale = Vector3.one * size;
        if (bulletRenderer == null) return;

        bulletRenderer.GetPropertyBlock(properties);
        properties.SetColor(EmissionColorId, color);
        bulletRenderer.SetPropertyBlock(properties);
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f || !isActiveAndEnabled) return;

        Projectile.Instance?.RemoveBullet(gameObject);
        gameObject.SetActive(false);
    }
}
