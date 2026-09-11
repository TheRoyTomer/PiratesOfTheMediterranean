using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Cannonball : MonoBehaviour
{
    [Header("Cannonball Settings")]
    [SerializeField] private float maxRange = 100f;
    [SerializeField] private float waterDeathHeight = -3f;

    private Rigidbody rb;
    private CannonballPool pool;

    private Vector3 startPosition;
    private bool reachedMaxRange;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Launch(
        CannonballPool cannonballPool,
        Vector3 direction,
        float speed)
    {
        pool = cannonballPool;

        startPosition = transform.position;
        reachedMaxRange = false;

        rb.linearVelocity = direction.normalized * speed;
        rb.angularVelocity = Vector3.zero;
    }

    private void FixedUpdate()
    {
        if (!reachedMaxRange)
        {
            float distanceTravelled =
                Vector3.Distance(startPosition, transform.position);

            if (distanceTravelled >= maxRange)
            {
                reachedMaxRange = true;

                // מפסיק את התנועה האופקית,
                // ומכאן הכדור נופל למים בגלל Gravity.
                rb.linearVelocity = new Vector3(
                    0f,
                    rb.linearVelocity.y,
                    0f
                );
            }
        }

        if (reachedMaxRange && transform.position.y <= waterDeathHeight)
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