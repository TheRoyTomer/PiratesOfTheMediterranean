using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Cannonball : MonoBehaviour
{
    [Header("Flight")]
    [SerializeField] private float upwardSpeed = 2.5f;
    [SerializeField] private float gravityStrength = 3.5f;

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

        if (transform.position.y <= waterDeathHeight ||
            lifeTimer >= maxLifetime)
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