using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Cannonball : MonoBehaviour
{
    //private Vector3 launchPosition;
    //private static float totalDistance;
    //private static int landedCannonballs;
    
    
    [Header("Flight")]
    [SerializeField] private float upwardSpeed = 1.2f;
    [SerializeField] private float gravityStrength = 4.7f;

    [Header("Pool")]
    [SerializeField] private float waterDeathHeight = -3f;
    [SerializeField] private float maxLifetime = 8f;

    private Rigidbody rb;
    private CannonballPool pool;

    private float lifeTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // We control gravity ourselves.
        rb.useGravity = false;
    }

    public void Launch(
        CannonballPool cannonballPool,
        Vector3 direction,
        float speed)
    {
        pool = cannonballPool;
        lifeTimer = 0f;
        
        //launchPosition = transform.position;
        
        rb.linearVelocity =
            direction.normalized * speed +
            Vector3.up * upwardSpeed;

        rb.angularVelocity = Vector3.zero;
    }

    private void FixedUpdate()
    {
        lifeTimer += Time.fixedDeltaTime;

        rb.AddForce(
            Vector3.down * gravityStrength,
            ForceMode.Acceleration
        );

        if (transform.position.y <= waterDeathHeight)
        {
            //LogDistance();
            ReturnToPool();
        }
        else if (lifeTimer >= maxLifetime)
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        pool.ReturnCannonball(gameObject);
    }
    
    
    // private void LogDistance()
    // {
    //     Vector3 start = Vector3.ProjectOnPlane(launchPosition, Vector3.up);
    //     Vector3 end = Vector3.ProjectOnPlane(transform.position, Vector3.up);
    //
    //     float distance = Vector3.Distance(start, end);
    //
    //     totalDistance += distance;
    //     landedCannonballs++;
    //
    //     float averageDistance = totalDistance / landedCannonballs;
    //
    //     Debug.Log(
    //         $"Cannonball distance: {distance:F1}m | " +
    //         $"Average: {averageDistance:F1}m | " +
    //         $"Samples: {landedCannonballs}"
    //     );
    // }
}