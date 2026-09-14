using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Cannonball : MonoBehaviour
{
    [Header("Cannonball Settings")]
    [SerializeField] private float maxRange = 100f;
    [SerializeField] private float upwardSpeed = 2f;

    [Header("Trajectory")]
    [SerializeField] private float gravityStartDistance = 65f;
    [SerializeField] private float gravityStrength = 8f;

    [Header("Pool")]
    [SerializeField] private float waterDeathHeight = -3f;

    private Rigidbody rb;
    private CannonballPool pool;
    private Vector3 startPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // אנחנו שולטים בנפילה בעצמנו
        rb.useGravity = false;
    }

    public void Launch(
        CannonballPool cannonballPool,
        Vector3 direction,
        float speed)
    {
        pool = cannonballPool;
        startPosition = transform.position;

        rb.linearVelocity =
            direction.normalized * speed
            + Vector3.up * upwardSpeed;

        rb.angularVelocity = Vector3.zero;
    }

    private void FixedUpdate()
    {
        float distanceTravelled =
            Vector3.Distance(startPosition, transform.position);

        // רק אחרי שהכדור עבר חלק גדול מהטווח,
        // מתחילים למשוך אותו משמעותית למטה.
        if (distanceTravelled >= gravityStartDistance)
        {
            rb.AddForce(
                Vector3.down * gravityStrength,
                ForceMode.Acceleration
            );
        }

        if (distanceTravelled >= maxRange &&
            transform.position.y <= waterDeathHeight)
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
}