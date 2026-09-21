using UnityEngine;

[RequireComponent(typeof(ShipController))]
[RequireComponent(typeof(WeaponSystem))]
public class AIController : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Steering")]
    [SerializeField] private float alignmentTolerance = 3f;
    [SerializeField] private float fullSteeringAngle = 30f;

    [Header("Movement")]
    [SerializeField] private float enterBroadsideRange = 325f;
    [SerializeField] private float exitBroadsideRange = 400f;
    [SerializeField] private float brakingDistance = 325f;
    [SerializeField] private float slowdownDistance = 450f;
    [SerializeField, Range(0f, 1f)] private float minimumApproachThrottle = 0.3f;

    [Header("Firing")]
    [SerializeField] private float maxCannonRange = 600f;
    [SerializeField] private float frontFireAngle = 5f;
    [SerializeField] private float broadsideFireAngle = 2.5f;

    [Header("Broadside Selection")]
    [SerializeField] private float broadsideSwitchThreshold = 15f;
    [SerializeField, Range(-1f, 0f)] private float broadsideBrakeThrottle = -0.35f;
    [SerializeField] private float broadsideAlignmentTolerance = 1f;
    [SerializeField] private float broadsideFullSteeringAngle = 7f;
    [SerializeField] private float broadsideBlockedTimeout = 1.5f;

    [Header("Patrol")]
    [SerializeField] private PatrolPoint patrolTarget;
    [SerializeField] private float patrolArrivalDistance = 20f;

    [Header("Detection")]
    [SerializeField] private float lostTargetGraceTime = 2f;
    [SerializeField] private float searchTimeout = 6f;
    [SerializeField] private float searchArrivalDistance = 100f;

    private float lostTargetTimer;
    private Vector3 lastKnownTargetPosition;
    private bool hasLastKnownTargetPosition;
    private float broadsideBlockedTimer;
    
    private float searchTimer;
    private bool reachedSearchArea;

    private PatrolPoint previousPatrolPoint;

    private ShipController ship;
    private WeaponSystem weapons;
    private AIPerception perception;
    private AIObstacleAvoidance obstacleAvoidance;

    private float desiredSteering;
    private float desiredThrottle;

    private CombatMode combatMode = CombatMode.Patrol;

    // Front means "no broadside has been chosen yet"
    private FiringDirection selectedBroadside = FiringDirection.Front;

    internal enum CombatMode
    {
        Patrol,
        Chase,
        Broadside,
        Reposition,
        Search
    }

    // State access keeps serialized tuning and shared runtime data on this component.
    internal Transform Target => target;
    internal float AlignmentTolerance => alignmentTolerance;
    internal float FullSteeringAngle => fullSteeringAngle;
    internal float ExitBroadsideRange => exitBroadsideRange;
    internal float BrakingDistance => brakingDistance;
    internal float SlowdownDistance => slowdownDistance;
    internal float MinimumApproachThrottle => minimumApproachThrottle;
    internal float MaxCannonRange => maxCannonRange;
    internal float FrontFireAngle => frontFireAngle;
    internal float BroadsideFireAngle => broadsideFireAngle;
    internal float BroadsideBrakeThrottle => broadsideBrakeThrottle;
    internal float BroadsideAlignmentTolerance => broadsideAlignmentTolerance;
    internal float BroadsideFullSteeringAngle => broadsideFullSteeringAngle;
    internal PatrolPoint PatrolTarget { get => patrolTarget; set => patrolTarget = value; }
    internal float PatrolArrivalDistance => patrolArrivalDistance;
    internal float SearchTimeout => searchTimeout;
    internal float SearchArrivalDistance => searchArrivalDistance;
    internal float LostTargetTimer { get => lostTargetTimer; set => lostTargetTimer = value; }
    internal Vector3 LastKnownTargetPosition => lastKnownTargetPosition;
    internal bool HasLastKnownTargetPosition { get => hasLastKnownTargetPosition; set => hasLastKnownTargetPosition = value; }
    internal float SearchTimer { get => searchTimer; set => searchTimer = value; }
    internal bool ReachedSearchArea { get => reachedSearchArea; set => reachedSearchArea = value; }
    internal PatrolPoint PreviousPatrolPoint { get => previousPatrolPoint; set => previousPatrolPoint = value; }
    internal WeaponSystem Weapons => weapons;
    internal AIPerception Perception => perception;
    internal float DesiredSteering { get => desiredSteering; set => desiredSteering = value; }
    internal float DesiredThrottle { get => desiredThrottle; set => desiredThrottle = value; }
    internal FiringDirection SelectedBroadside { get => selectedBroadside; set => selectedBroadside = value; }

    private PatrolState patrolState;
    private ChaseState chaseState;
    private BroadsideState broadsideState;
    private RepositionState repositionState;
    private SearchState searchState;

    private void Awake()
    {
        ship = GetComponent<ShipController>();
        weapons = GetComponent<WeaponSystem>();
        perception = GetComponent<AIPerception>();
        obstacleAvoidance = GetComponent<AIObstacleAvoidance>();

        patrolState = new PatrolState(this);
        chaseState = new ChaseState(this);
        broadsideState = new BroadsideState(this);
        repositionState = new RepositionState(this);
        searchState = new SearchState(this);
    }

    private void FixedUpdate()
{
    desiredSteering = 0f;
    desiredThrottle = 0f;

    Vector3 toTarget = Vector3.zero;
    float distance = 0f;

    if (combatMode == CombatMode.Patrol &&
        perception.CanSeeTarget(
            target,
            perception.DetectionRange))
    {
        ChangeState(CombatMode.Chase);
    }

    bool targetVisible = true;

    if (combatMode != CombatMode.Patrol)
    {
        if (target == null)
        {
            ship.SetSteering(0f);
            ship.SetThrottle(0f);
            return;
        }

        toTarget = target.position - transform.position;
        distance = toTarget.magnitude;

        targetVisible = UpdateTargetVisibility();
    }

    if (combatMode != CombatMode.Patrol &&
        combatMode != CombatMode.Search &&
        !targetVisible &&
        lostTargetTimer >= lostTargetGraceTime)
    {
        selectedBroadside = FiringDirection.Front;
        ChangeState(CombatMode.Search);
    }

    if (combatMode == CombatMode.Patrol)
    {
        patrolState.Tick(toTarget, distance);
    }
    else if (combatMode == CombatMode.Chase)
    {
        if (distance <= enterBroadsideRange)
        {
            bool broadsideAvailable =
                TryChooseBroadside(toTarget);

            if (broadsideAvailable)
            {
                ChangeState(CombatMode.Broadside);
                broadsideState.Tick(toTarget, distance);
            }
            else
            {
                selectedBroadside = FiringDirection.Front;
                ChangeState(CombatMode.Reposition);
            }
        }
        else
        {
            chaseState.Tick(toTarget, distance);
        }
    }
    else if (combatMode == CombatMode.Broadside)
    {
        if (distance >= exitBroadsideRange)
        {
            ChangeState(CombatMode.Chase);
            chaseState.Tick(toTarget, distance);
        }
        else
        {
            broadsideState.Tick(toTarget, distance);
        }
    }
    else if (combatMode == CombatMode.Reposition)
    {
        repositionState.Tick(toTarget, distance);
    }
    else if (combatMode == CombatMode.Search)
    {
        searchState.Tick(toTarget, distance);
    }

    bool avoidanceActive =
        obstacleAvoidance.ApplyAvoidance(
            ref desiredSteering,
            ref desiredThrottle
        );

    if (combatMode == CombatMode.Broadside &&
        avoidanceActive)
    {
        broadsideBlockedTimer += Time.fixedDeltaTime;

        if (broadsideBlockedTimer >=
            broadsideBlockedTimeout)
        {
            broadsideBlockedTimer = 0f;
            selectedBroadside = FiringDirection.Front;
            ChangeState(CombatMode.Reposition);
        }
    }
    else
    {
        broadsideBlockedTimer = 0f;
    }

    ship.SetSteering(desiredSteering);
    ship.SetThrottle(desiredThrottle);
}

    internal void ChangeState(CombatMode newState)
    {
        if (combatMode == newState)
            return;

        combatMode = newState;

        if (combatMode == CombatMode.Search)
        {
            searchTimer = 0f;
            reachedSearchArea = false;
        }

        Debug.Log($"AI State -> {combatMode}");
    }

    private bool TryChooseBroadside(Vector3 toTarget)
{
    float rightAngle = Vector3.Angle(
        transform.right,
        toTarget
    );

    float leftAngle = Vector3.Angle(
        -transform.right,
        toTarget
    );

    FiringDirection preferredSide;

    if (selectedBroadside == FiringDirection.Front)
    {
        preferredSide =
            rightAngle < leftAngle
                ? FiringDirection.Right
                : FiringDirection.Left;
    }
    else if (selectedBroadside == FiringDirection.Right)
    {
        preferredSide =
            leftAngle + broadsideSwitchThreshold < rightAngle
                ? FiringDirection.Left
                : FiringDirection.Right;
    }
    else
    {
        preferredSide =
            rightAngle + broadsideSwitchThreshold < leftAngle
                ? FiringDirection.Right
                : FiringDirection.Left;
    }

    FiringDirection alternateSide =
        preferredSide == FiringDirection.Right
            ? FiringDirection.Left
            : FiringDirection.Right;

    bool preferredBlocked =
        IsBroadsideManeuverBlocked(
            preferredSide,
            toTarget
        );

    bool alternateBlocked =
        IsBroadsideManeuverBlocked(
            alternateSide,
            toTarget
        );

    if (!preferredBlocked)
    {
        selectedBroadside = preferredSide;
        return true;
    }

    if (!alternateBlocked)
    {
        selectedBroadside = alternateSide;
        return true;
    }

    return false;
}

private bool IsBroadsideManeuverBlocked(
    FiringDirection side,
    Vector3 toTarget)
{
    Vector3 sideDirection =
        side == FiringDirection.Right
            ? transform.right
            : -transform.right;

    float alignmentError = Vector3.SignedAngle(
        sideDirection,
        toTarget,
        Vector3.up
    );

    float probeAngle = Mathf.Min(
        Mathf.Abs(alignmentError),
        45f
    );

    float turnDirection =
        Mathf.Sign(alignmentError);

    Vector3 flatForward =
        Vector3.ProjectOnPlane(
            transform.forward,
            Vector3.up
        ).normalized;

    Vector3 probeDirection =
        Quaternion.Euler(
            0f,
            turnDirection * probeAngle,
            0f
        ) * flatForward;

    return obstacleAvoidance.IsDirectionBlocked(
        probeDirection
    );
}

    internal float CalculateSteering(
        float angle,
        float tolerance,
        float fullAngle)
    {
        if (Mathf.Abs(angle) <= tolerance)
        {
            return 0f;
        }

        float magnitude = Mathf.InverseLerp(
            tolerance,
            fullAngle,
            Mathf.Abs(angle)
        );

        return Mathf.Sign(angle) * magnitude;
    }

    private bool UpdateTargetVisibility()
    {
        if (perception.CanSeeTarget(
                target,
                perception.LoseTargetRange))
        {
            lostTargetTimer = 0f;
            lastKnownTargetPosition = target.position;
            hasLastKnownTargetPosition = true;
            return true;
        }

        lostTargetTimer += Time.fixedDeltaTime;
        return false;
    }
    
    
}
