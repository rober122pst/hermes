using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Boresight : MonoBehaviour
{
    [SerializeField] private Transform rayOrigin;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private RectTransform uiDot;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private LayerMask collisionMask;

    [Header("Assistência magnética")]
    [SerializeField, Range(1f, 35f)] private float acquireAngle = 16f;
    [SerializeField, Range(1f, 40f)] private float releaseAngle = 21f;

    private static readonly Vector2 ViewportCenter = new(0.5f, 0.5f);
    private static readonly Color IdleColor = new(0.86f, 0.94f, 1f, 0.9f);
    private static readonly Color LockColor = new(0.45f, 1f, 0.89f);
    private static readonly Color HitColor = new(1f, 0.79f, 0.63f);

    private Graphic centerGraphic;
    private RectTransform lockMarker;
    private readonly Image[] lockStrokes = new Image[8];
    private Camera canvasCamera;
    private RawImage presentation;
    private Camera presentationCamera;
    private EnemySystem target;
    private float impactUntil;

    public bool HasTarget => target != null && target.IsAlive;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (uiDot == null || canvasRect == null)
            return;

        centerGraphic = uiDot.GetComponent<Graphic>();
        uiDot.sizeDelta = new Vector2(8f, 8f);
        uiDot.gameObject.AddComponent<Outline>().effectColor = new Color(0.02f, 0.03f, 0.08f, 0.85f);

        canvasCamera = GetCanvasCamera(canvasRect);
        if (mainCamera != null && mainCamera.targetTexture != null)
        {
            foreach (RawImage image in FindObjectsByType<RawImage>(FindObjectsSortMode.None))
            {
                if (image.texture != mainCamera.targetTexture)
                    continue;

                presentation = image;
                presentationCamera = GetCanvasCamera(image.rectTransform);
                break;
            }
        }

        lockMarker = new GameObject("Alvo do laser", typeof(RectTransform)).GetComponent<RectTransform>();
        lockMarker.SetParent(uiDot.parent, false);
        lockMarker.SetSiblingIndex(uiDot.GetSiblingIndex() + 1);
        lockMarker.anchorMin = ViewportCenter;
        lockMarker.anchorMax = ViewportCenter;
        lockMarker.sizeDelta = Vector2.zero;
        CreateLockBracket();
        ClearAim();
    }

    public Vector3 ResolveAim(float projectileSpeed, float projectileLifetime, bool firing)
    {
        Vector3 origin = rayOrigin != null ? rayOrigin.position : transform.position;
        float maxRange = projectileSpeed * projectileLifetime;
        if (mainCamera == null)
            return origin + transform.forward * maxRange;

        Ray centerRay = mainCamera.ViewportPointToRay(ViewportCenter);
        target = SelectTarget(centerRay, origin, maxRange);
        RefreshReticles();

        if (target != null)
        {
            Vector3 targetPoint = target.AimPoint;
            Vector3 offset = targetPoint - origin;
            Vector3 velocity = target.Velocity;
            float travelTime = offset.magnitude / projectileSpeed;
            travelTime = (offset + velocity * travelTime).magnitude / projectileSpeed;
            return targetPoint + velocity * Mathf.Min(travelTime, projectileLifetime);
        }

        float cameraToMuzzle = Mathf.Max(0f, Vector3.Dot(origin - centerRay.origin, centerRay.direction));
        if (firing && Physics.Raycast(centerRay, out RaycastHit hit, maxRange + cameraToMuzzle,
                collisionMask, QueryTriggerInteraction.Collide)
            && hit.distance > cameraToMuzzle)
            return hit.point;

        return centerRay.GetPoint(cameraToMuzzle + Mathf.Min(maxDistance, maxRange));
    }

    public void ConfirmImpact()
    {
        impactUntil = Time.unscaledTime + 0.11f;
    }

    public void ClearAim()
    {
        target = null;
        if (uiDot != null)
            uiDot.gameObject.SetActive(false);
        if (lockMarker != null)
            lockMarker.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        ClearAim();
    }

    private void OnDestroy()
    {
        if (lockMarker != null)
            Destroy(lockMarker.gameObject);
    }

    private EnemySystem SelectTarget(Ray centerRay, Vector3 muzzle, float range)
    {
        EnemySystem bestTarget = null;
        float bestScore = float.PositiveInfinity;
        float acquireDot = Mathf.Cos(acquireAngle * Mathf.Deg2Rad);
        float retainDot = Mathf.Cos(Mathf.Max(acquireAngle, releaseAngle) * Mathf.Deg2Rad);
        float rangeSquared = range * range;
        IReadOnlyList<EnemySystem> enemies = EnemySystem.ActiveEnemies;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemySystem candidate = enemies[i];
            if (candidate == null || !candidate.CanTakeDamage)
                continue;

            Vector3 point = candidate.AimPoint;
            Vector3 fromMuzzle = point - muzzle;
            float distanceSquared = fromMuzzle.sqrMagnitude;
            if (distanceSquared > rangeSquared || Vector3.Dot(fromMuzzle, centerRay.direction) <= 0f)
                continue;

            float angleDot = Vector3.Dot((point - centerRay.origin).normalized, centerRay.direction);
            bool retained = candidate == target;
            if (angleDot < (retained ? retainDot : acquireDot))
                continue;

            Vector3 viewport = mainCamera.WorldToViewportPoint(point);
            if (viewport.z <= 0f || viewport.x < 0.02f || viewport.x > 0.98f
                || viewport.y < 0.02f || viewport.y > 0.98f)
                continue;

            float score = (1f - angleDot) / (1f - acquireDot) + 0.3f * distanceSquared / rangeSquared;
            if (retained)
                score = score * 0.7f - 0.22f;

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = candidate;
            }
        }

        return bestTarget;
    }

    private void RefreshReticles()
    {
        if (centerGraphic == null || lockMarker == null)
            return;

        uiDot.gameObject.SetActive(true);
        PlaceMarker(uiDot, ViewportCenter);
        bool impact = Time.unscaledTime < impactUntil;
        centerGraphic.color = impact ? HitColor : HasTarget ? LockColor : IdleColor;
        uiDot.localScale = impact ? Vector3.one * 1.4f : Vector3.one;

        bool hasTarget = HasTarget;
        lockMarker.gameObject.SetActive(hasTarget);
        if (!hasTarget)
            return;

        Vector3 viewport = mainCamera.WorldToViewportPoint(target.AimPoint);
        PlaceMarker(lockMarker, viewport);
        for (int i = 0; i < lockStrokes.Length; i++)
            lockStrokes[i].color = impact ? HitColor : LockColor;
    }

    private void PlaceMarker(RectTransform marker, Vector2 viewport)
    {
        Vector2 screenPoint;
        if (presentation != null && presentation.isActiveAndEnabled)
        {
            Rect imageRect = presentation.rectTransform.rect;
            Rect uv = presentation.uvRect;
            Vector2 imagePoint = new((viewport.x - uv.x) / uv.width, (viewport.y - uv.y) / uv.height);
            Vector3 worldPoint = presentation.rectTransform.TransformPoint(Rect.NormalizedToPoint(imageRect, imagePoint));
            screenPoint = RectTransformUtility.WorldToScreenPoint(presentationCamera, worldPoint);
        }
        else if (mainCamera.targetTexture == null)
        {
            screenPoint = Rect.NormalizedToPoint(mainCamera.pixelRect, viewport);
        }
        else
        {
            screenPoint = new Vector2(viewport.x * Screen.width, viewport.y * Screen.height);
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, canvasCamera, out Vector2 local))
            marker.localPosition = local;
    }

    private void CreateLockBracket()
    {
        for (int i = 0; i < 4; i++)
        {
            float x = (i & 1) == 0 ? -1f : 1f;
            float y = (i & 2) == 0 ? -1f : 1f;
            lockStrokes[i * 2] = CreateStroke(new Vector2(x * 10f, y * 13f), new Vector2(8f, 2f));
            lockStrokes[i * 2 + 1] = CreateStroke(new Vector2(x * 13f, y * 10f), new Vector2(2f, 8f));
        }
    }

    private Image CreateStroke(Vector2 position, Vector2 size)
    {
        var stroke = new GameObject("Traço", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        var image = stroke.GetComponent<Image>();
        image.raycastTarget = false;
        image.rectTransform.SetParent(lockMarker, false);
        image.rectTransform.anchoredPosition = position;
        image.rectTransform.sizeDelta = size;
        stroke.GetComponent<Outline>().effectColor = new Color(0.02f, 0.03f, 0.08f, 0.85f);
        return image;
    }

    private static Camera GetCanvasCamera(RectTransform rect)
    {
        Canvas canvas = rect.GetComponentInParent<Canvas>()?.rootCanvas;
        return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
    }
}
