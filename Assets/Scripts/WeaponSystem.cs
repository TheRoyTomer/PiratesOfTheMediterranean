using UnityEngine;

public class WeaponSystem : MonoBehaviour
{
    [Header("Fire Points")]
    [SerializeField] private Transform[] frontFirePoints;
    [SerializeField] private Transform[] leftFirePoints;
    [SerializeField] private Transform[] rightFirePoints;

    [Header("Cannon Settings")]
    [SerializeField] private CannonballPool cannonballPool;
    [SerializeField] private float cannonballSpeed = 30f;

    public void Fire(FiringDirection direction)
    {
        Transform[] selectedFirePoints = GetFirePoints(direction);

        foreach (Transform firePoint in selectedFirePoints)
        {
            FireCannonball(firePoint);
        }
    }

    private Transform[] GetFirePoints(FiringDirection direction)
    {
        return direction switch
        {
            FiringDirection.Front => frontFirePoints,
            FiringDirection.Left => leftFirePoints,
            FiringDirection.Right => rightFirePoints,
            _ => System.Array.Empty<Transform>()
        };
    }

    private void FireCannonball(Transform firePoint)
    {
        GameObject cannonballObject = cannonballPool.GetCannonball();
        Debug.Log($"Firing cannonball from {firePoint.name} at {firePoint.position}");
        Cannonball cannonball = cannonballObject.GetComponent<Cannonball>();

        cannonballObject.transform.position = firePoint.position;
        cannonballObject.transform.rotation = firePoint.rotation;

        cannonball.Launch(
            cannonballPool,
            firePoint.forward,
            cannonballSpeed
        );
    }
}