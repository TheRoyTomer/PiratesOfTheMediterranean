using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(ShipController))]
public sealed class ShipContactRecovery : MonoBehaviour
{
    [Header("Impact Recovery")]
    [SerializeField] private float recoveryKickSpeed = 30f;
    [SerializeField] private float throttleLockDuration = 0.35f;

    [Tooltip("1 = directly ahead, 0 = exactly sideways.")]
    [SerializeField, Range(0f, 1f)]
    private float frontContactDot = 0.6f;

    [SerializeField, Range(0f, 1f)]
    private float frontNormalDot = 0.5f;

    private Rigidbody body;
    private ShipController shipController;

    private int shipPhysicalLayer;
    private int shipCollisionLayer;
    private int shorelineLayer;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        shipController = GetComponent<ShipController>();

        shipPhysicalLayer = LayerMask.NameToLayer("ShipPhysical");
        shipCollisionLayer = LayerMask.NameToLayer("ShipCollision");
        shorelineLayer = LayerMask.NameToLayer("ShorelineBoundary");
    }

    private void OnCollisionEnter(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);

            if (!TryGetOwnAndOtherCollider(
                    contact,
                    out Collider ownCollider,
                    out Collider otherCollider))
            {
                continue;
            }

            if (!IsRecoveryRelevantContact(
                    ownCollider,
                    otherCollider))
            {
                continue;
            }

            if (!IsFrontContact(contact))
                continue;

            shipController.StartContactRecovery(
                recoveryKickSpeed,
                throttleLockDuration
            );

            return;
        }
    }

    private bool TryGetOwnAndOtherCollider(
        ContactPoint contact,
        out Collider ownCollider,
        out Collider otherCollider)
    {
        ownCollider = contact.thisCollider;
        otherCollider = contact.otherCollider;

        if (ownCollider != null &&
            ownCollider.attachedRigidbody == body)
        {
            return true;
        }

        if (otherCollider != null &&
            otherCollider.attachedRigidbody == body)
        {
            Collider temp = ownCollider;
            ownCollider = otherCollider;
            otherCollider = temp;

            return true;
        }

        return false;
    }

    private bool IsRecoveryRelevantContact(
        Collider ownCollider,
        Collider otherCollider)
    {
        if (ownCollider == null ||
            otherCollider == null)
        {
            return false;
        }

        int ownLayer = ownCollider.gameObject.layer;
        int otherLayer = otherCollider.gameObject.layer;

        bool shorelineContact =
            ownLayer == shipPhysicalLayer &&
            otherLayer == shorelineLayer;

        if (shorelineContact)
            return true;

        bool shipContact =
            ownLayer == shipCollisionLayer &&
            otherLayer == shipCollisionLayer &&
            otherCollider.attachedRigidbody != null &&
            otherCollider.attachedRigidbody != body;

        return shipContact;
    }

    private bool IsFrontContact(ContactPoint contact)
    {
        Vector3 flatForward =
            Vector3.ProjectOnPlane(
                transform.forward,
                Vector3.up
            );

        Vector3 toContact =
            contact.point - transform.position;

        toContact.y = 0f;

        if (flatForward.sqrMagnitude <= 0.001f ||
            toContact.sqrMagnitude <= 0.001f)
        {
            return false;
        }

        flatForward.Normalize();
        toContact.Normalize();

        float positionDot =
            Vector3.Dot(
                flatForward,
                toContact
            );

        if (positionDot < frontContactDot)
            return false;

        Vector3 normal = contact.normal;

        if (contact.thisCollider != null &&
            contact.thisCollider.attachedRigidbody != body)
        {
            normal = -normal;
        }

        normal.y = 0f;

        if (normal.sqrMagnitude <= 0.001f)
            return false;

        normal.Normalize();

        float normalDot =
            Vector3.Dot(
                flatForward,
                -normal
            );

        return normalDot >= frontNormalDot;
    }
}