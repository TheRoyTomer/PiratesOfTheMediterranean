using UnityEngine;

[RequireComponent(typeof(ShipController))]
[RequireComponent(typeof(WeaponSystem))]
[RequireComponent(typeof(AIRammingEvaluator))]
[RequireComponent(typeof(AIEvadeEvaluator))]
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
    [SerializeField] private Transform patrolPointsRoot;

    [Header("Detection")]
    [SerializeField] private float lostTargetGraceTime = 2f;
    [SerializeField] private float searchTimeout = 6f;
    [SerializeField] private float searchArrivalDistance = 100f;

    [Header("Evade")]
    [SerializeField] private float evadeMinimumTime = 12f;
    [SerializeField] private float evadeMaximumTime = 25f;
    [SerializeField] private float evadeNoDamageExitTime = 4f;

    [Header("Ramming")]
    [SerializeField] private float ramTerminalAdjustDistance = 150f;
    [SerializeField] private float ramTerminalAdjustMaxAngle = 20f;

    private Vector3 pendingRamInterceptPoint;

    private PatrolPoint[] allPatrolPoints;

    private float evadeTimer;

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
    private ShipHealth shipHealth;
    private AIRammingEvaluator rammingEvaluator;
    private AIEvadeEvaluator evadeEvaluator;

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
        Search,
        Evade,
        Ramming,
        BreakSteer
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

    internal PatrolPoint PatrolTarget
    {
        get => patrolTarget;
        set => patrolTarget = value;
    }

    internal float PatrolArrivalDistance => patrolArrivalDistance;
    internal float SearchTimeout => searchTimeout;
    internal float SearchArrivalDistance => searchArrivalDistance;

    internal float LostTargetTimer
    {
        get => lostTargetTimer;
        set => lostTargetTimer = value;
    }

    internal Vector3 LastKnownTargetPosition => lastKnownTargetPosition;

    internal bool HasLastKnownTargetPosition
    {
        get => hasLastKnownTargetPosition;
        set => hasLastKnownTargetPosition = value;
    }

    internal float SearchTimer
    {
        get => searchTimer;
        set => searchTimer = value;
    }

    internal bool ReachedSearchArea
    {
        get => reachedSearchArea;
        set => reachedSearchArea = value;
    }

    internal PatrolPoint PreviousPatrolPoint
    {
        get => previousPatrolPoint;
        set => previousPatrolPoint = value;
    }

    internal WeaponSystem Weapons => weapons;
    internal AIPerception Perception => perception;
    internal AIObstacleAvoidance ObstacleAvoidance => obstacleAvoidance;

    internal float DesiredSteering
    {
        get => desiredSteering;
        set => desiredSteering = value;
    }

    internal float DesiredThrottle
    {
        get => desiredThrottle;
        set => desiredThrottle = value;
    }

    internal FiringDirection SelectedBroadside
    {
        get => selectedBroadside;
        set => selectedBroadside = value;
    }

    internal float LastDamageTime => evadeEvaluator.LastDamageTime;
    internal float EvadeMinimumTime => evadeMinimumTime;
    internal float EvadeMaximumTime => evadeMaximumTime;
    internal float EvadeNoDamageExitTime => evadeNoDamageExitTime;

    internal float EvadeTimer
    {
        get => evadeTimer;
        set => evadeTimer = value;
    }

    internal PatrolPoint[] AllPatrolPoints => allPatrolPoints;

    internal Vector3 PendingRamInterceptPoint => pendingRamInterceptPoint;
    internal float RamTerminalAdjustDistance => ramTerminalAdjustDistance;
    internal float RamTerminalAdjustMaxAngle => ramTerminalAdjustMaxAngle;

    private PatrolState patrolState;
    private ChaseState chaseState;
    private BroadsideState broadsideState;
    private RepositionState repositionState;
    private SearchState searchState;
    private EvadeState evadeState;
    private RammingState rammingState;
    private BreakSteerState breakSteerState;

    private CombatMode breakSteerReturnState;

    private void Awake()
    {
        ship = GetComponent<ShipController>();
        rammingEvaluator = GetComponent<AIRammingEvaluator>();
        evadeEvaluator = GetComponent<AIEvadeEvaluator>();
        weapons = GetComponent<WeaponSystem>();
        perception = GetComponent<AIPerception>();
        obstacleAvoidance = GetComponent<AIObstacleAvoidance>();
        shipHealth = GetComponent<ShipHealth>();

        if (patrolPointsRoot != null)
        {
            allPatrolPoints =
                patrolPointsRoot.GetComponentsInChildren<PatrolPoint>();
        }

        shipHealth.OnDamaged += HandleDamage;

        patrolState = new PatrolState(this);
        chaseState = new ChaseState(this);
        broadsideState = new BroadsideState(this);
        repositionState = new RepositionState(this);
        searchState = new SearchState(this);
        evadeState = new EvadeState(this);
        rammingState = new RammingState(this);
        breakSteerState = new BreakSteerState(this);
    }

    private void OnDestroy()
    {
        if (shipHealth != null)
        {
            shipHealth.OnDamaged -= HandleDamage;
        }
    }

    private void FixedUpdate()
    {
        desiredSteering = 0f;
        desiredThrottle = 0f;

        evadeEvaluator.Tick(Time.fixedDeltaTime);
        rammingEvaluator.Tick(Time.fixedDeltaTime);

        Vector3 toTarget = Vector3.zero;
        float distance = 0f;

        if (target != null)
        {
            toTarget = target.position - transform.position;
            distance = toTarget.magnitude;
        }

        if (combatMode == CombatMode.Patrol &&
            perception.CanSeeTargetFromPatrol(
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

            targetVisible = UpdateTargetVisibility();
        }

        bool breakSteerEligible =
            combatMode != CombatMode.Patrol &&
            combatMode != CombatMode.Broadside &&
            combatMode != CombatMode.Ramming &&
            combatMode != CombatMode.BreakSteer;

        breakSteerState.UpdatePursuit(
            toTarget,
            distance,
            target != null && breakSteerEligible
        );

        if (breakSteerEligible &&
            breakSteerState.ShouldEnter)
        {
            TryEnterBreakSteer(toTarget);
        }

        if (combatMode != CombatMode.Patrol &&
            combatMode != CombatMode.Search &&
            combatMode != CombatMode.Evade &&
            combatMode != CombatMode.Ramming &&
            combatMode != CombatMode.BreakSteer &&
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
            if (TryEnterRamming())
            {
                rammingState.Tick(toTarget, distance);
            }
            else if (distance <= enterBroadsideRange)
            {
                bool broadsideAvailable =
                    TryChooseBroadside(toTarget);

                if (broadsideAvailable)
                {
                    broadsideState.Begin(toTarget);

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
            if (TryEnterRamming())
            {
                rammingState.Tick(toTarget, distance);
            }
            else
            {
                repositionState.Tick(toTarget, distance);
            }
        }
        else if (combatMode == CombatMode.Search)
        {
            searchState.Tick(toTarget, distance);
        }
        else if (combatMode == CombatMode.Evade)
        {
            evadeState.Tick(toTarget, distance);
        }
        else if (combatMode == CombatMode.Ramming)
        {
            rammingState.Tick(toTarget, distance);
        }
        else if (combatMode == CombatMode.BreakSteer)
        {
            breakSteerState.Tick(toTarget, distance);
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

        bool suspendedEvade =
            combatMode == CombatMode.BreakSteer &&
            breakSteerReturnState == CombatMode.Evade;

        CombatMode previousState = combatMode;

        if (previousState == CombatMode.BreakSteer)
        {
            breakSteerState.Cancel();
        }

        combatMode = newState;

        if (previousState == CombatMode.Evade ||
            suspendedEvade)
        {
            evadeEvaluator.NotifyEvadeEnded();
        }

        if (previousState == CombatMode.Ramming)
        {
            rammingEvaluator.NotifyRammingEnded();
        }

        if (combatMode == CombatMode.Search)
        {
            searchTimer = 0f;
            reachedSearchArea = false;
        }

        if (combatMode == CombatMode.Evade)
        {
            evadeTimer = 0f;
            evadeEvaluator.NotifyEvadeStarted();
            evadeState.Reset();
        }

        if (combatMode == CombatMode.Ramming)
        {
            rammingState.Begin(pendingRamInterceptPoint);
        }
        
        if (combatMode == CombatMode.Reposition)
        {
            repositionState.Begin();
        }
        
        Debug.Log(
            $"[{Time.time:F2}s] AI State: {previousState} -> {combatMode}"
        );
    }

    private bool TryEnterBreakSteer(Vector3 toTarget)
    {
        if (combatMode == CombatMode.Broadside ||
            combatMode == CombatMode.Ramming ||
            combatMode == CombatMode.BreakSteer)
        {
            return false;
        }

        CombatMode previousState = combatMode;

        if (!breakSteerState.Begin(toTarget))
            return false;

        breakSteerReturnState = previousState;
        combatMode = CombatMode.BreakSteer;

        Debug.Log(
            $"AI State -> BreakSteer (return: {breakSteerReturnState})"
        );

        return true;
    }

    internal void ReturnFromBreakSteer()
    {
        if (combatMode != CombatMode.BreakSteer)
            return;

        CombatMode returnState =
            breakSteerReturnState;

        breakSteerState.Cancel();

        combatMode = returnState;

        Debug.Log(
            $"AI State -> {combatMode} (returned from BreakSteer)"
        );
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

    private void HandleDamage(float damage)
    {
        bool wasPatrolling =
            combatMode == CombatMode.Patrol;

        bool isEffectivelyEvading =
            combatMode == CombatMode.Evade ||
            (combatMode == CombatMode.BreakSteer &&
             breakSteerReturnState == CombatMode.Evade);

        bool canEnterEvade =
            combatMode != CombatMode.Broadside &&
            !isEffectivelyEvading &&
            combatMode != CombatMode.Ramming;

        AIEvadeEvaluator.DamageDecision decision =
            evadeEvaluator.EvaluateDamage(
                shipHealth,
                isEffectivelyEvading,
                canEnterEvade,
                evadeTimer
            );

        if (decision ==
            AIEvadeEvaluator.DamageDecision.ExitEvadeToChase)
        {
            ChangeState(CombatMode.Chase);
            return;
        }

        if (decision ==
            AIEvadeEvaluator.DamageDecision.EnterEvade)
        {
            selectedBroadside =
                FiringDirection.Front;

            ChangeState(CombatMode.Evade);
            return;
        }

        if (wasPatrolling)
        {
            ChangeState(CombatMode.Chase);
        }
    }

    internal PatrolPoint ChooseEvadePatrolPoint()
    {
        if (allPatrolPoints == null ||
            allPatrolPoints.Length == 0 ||
            target == null)
        {
            return null;
        }

        PatrolPoint bestPoint = null;
        float bestScore = Mathf.NegativeInfinity;

        foreach (PatrolPoint point in allPatrolPoints)
        {
            if (point == null)
                continue;

            float distanceFromAI =
                Vector3.Distance(
                    transform.position,
                    point.transform.position
                );

            float distanceFromPlayer =
                Vector3.Distance(
                    target.position,
                    point.transform.position
                );

            float score =
                distanceFromAI +
                distanceFromPlayer;

            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = point;
            }
        }

        return bestPoint;
    }

    private bool TryEnterRamming()
    {
        if (!rammingEvaluator.TryChooseRamming(
                target,
                out Vector3 interceptPoint))
        {
            return false;
        }

        pendingRamInterceptPoint = interceptPoint;

        ChangeState(CombatMode.Ramming);

        return true;
    }
    
    internal bool ShouldChaseAfterEvade()
    {
        if (target == null)
            return false;

        bool visibleWithCombatAwareness =
            perception.CanSeeTarget(
                target,
                perception.LoseTargetRange
            );

        if (!visibleWithCombatAwareness)
            return false;

        bool visibleFromPatrol =
            perception.CanSeeTargetFromPatrol(
                target,
                perception.LoseTargetRange
            );

        return !visibleFromPatrol;
    }
}