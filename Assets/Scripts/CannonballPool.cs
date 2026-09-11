using System.Collections.Generic;
using UnityEngine;

public class CannonballPool : MonoBehaviour
{
    [SerializeField] private GameObject cannonballPrefab;
    [SerializeField] private int initialPoolSize = 20;

    private readonly Queue<GameObject> pool = new Queue<GameObject>();

    private void Awake()
    {
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