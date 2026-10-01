using System.Collections.Generic;
using UnityEngine;

public class CannonballPool : MonoBehaviour
{
    [SerializeField] private GameObject cannonballPrefab;
    [SerializeField] private int initialPoolSize = 20;

    private readonly Queue<GameObject> pool = new Queue<GameObject>();
    public Cannonball Projectile => cannonballPrefab != null ? cannonballPrefab.GetComponent<Cannonball>() : null;
    private VfxPool effects;
    public VfxPool Effects => effects != null ? effects :
        (effects = GetComponent<VfxPool>() ?? gameObject.AddComponent<VfxPool>());

    private void Awake()
    {
        if (Projectile != null) Projectile.PrewarmEffects(Effects);
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject cannonball = CreateCannonball();
            cannonball.SetActive(false);
            pool.Enqueue(cannonball);
        }
    }

    public GameObject GetCannonball()
    {
        if (pool.Count > 0)
        {
            GameObject cannonball = pool.Dequeue();
            cannonball.SetActive(true);
            return cannonball;
        }

        return CreateCannonball();
    }

    public void ReturnCannonball(GameObject cannonball)
    {
        cannonball.SetActive(false);
        cannonball.transform.SetParent(transform);

        pool.Enqueue(cannonball);
    }

    private GameObject CreateCannonball()
    {
        return Instantiate(cannonballPrefab, transform);
    }
}
