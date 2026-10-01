using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public static Projectile Instance { get; private set; }
    private List<(GameObject bullet, float lifetime)> activeBullets = new List<(GameObject, float)>();
    public float lifetime = 2f;
    public float speed = 1.5f;

    void Start()
    {
        Instance = this;
    }

    public void AddBullet(GameObject bullet)
    {
        activeBullets.Add((bullet, lifetime));
    }

    public void RemoveBullet(GameObject bullet)
    {
        for (int i = activeBullets.Count - 1; i >= 0; i--)
        {
            if (activeBullets[i].bullet == bullet)
            {
                activeBullets.RemoveAt(i);
                break;
            }
        }
    }

    void Update()
    {
        UpdateBullets();
    }

    public void UpdateBullets()
    {
        for (int i = activeBullets.Count - 1; i >= 0; i--)
        {
            var activeBullet = activeBullets[i];
            if (activeBullet.bullet == null) continue;

            Vector3 direction = activeBullet.bullet.transform.forward;
            float distance = speed * Time.deltaTime;

            activeBullet.bullet.transform.position += direction * distance;

            activeBullet.lifetime -= Time.deltaTime;
            activeBullets[i] = activeBullet;

            if (activeBullets[i].lifetime <= 0f)
            {
                activeBullets[i].bullet.SetActive(false);
                activeBullets.RemoveAt(i);
            }
        }
    }
}
