using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Collider))]
public class HunterAgent : Agent
{
    public enum HunterState { Search, Attack, Gather, Flee, Exit, Dead }

    public static HunterAgent Current { get; private set; }
    public static event Action<bool, bool> AvailabilityChanged = delegate { };

    [Header("Movement")]
    [FormerlySerializedAs("moveSpeed")]
    [SerializeField] private float _moveSpeed = 5.2f;
    [SerializeField] private float _fleeSpeed = 6.2f;
    [SerializeField] private float _turnSpeed = 720f;
    [SerializeField] private float _maxSteering = 24f;

    [Header("Perception (boids and wolves)")]
    [Tooltip("Perception radius for live boids, eliminated boids and wolves, blocked by walls.")]
    [FormerlySerializedAs("perceptionRadius")]
    [SerializeField] private float _visionRadius = 14f;
    [SerializeField] private float _eyeHeight = 0.6f;
    [Tooltip("Start attacking slightly inside the vision limit to avoid repeated acquisition and loss at its edge.")]
    [SerializeField] private float _targetAcquireMargin = 1f;

    [Header("Hunter requirements")]
    [Tooltip("Required gathers before moving to the exit.")]
    [field: SerializeField] public int RequiredGathers { get; private set; } = 3;
    [Tooltip("Respawn delay after being captured by a wolf.")]
    [field: SerializeField] public float RespawnTime { get; private set; } = 5f;
    [Tooltip("Safe zone wait duration before resuming activity.")]
    [field: SerializeField] public float SafeZoneWaitTime { get; private set; } = 4f;

    [Header("Attack")]
    [field: SerializeField] public float TBA { get; private set; } = 1.5f;
    [field: SerializeField] public float MeleeAttackRadius { get; private set; } = 2f;
    [field: SerializeField] public float RangeAttackRadius { get; private set; } = 7f;
    [SerializeField] private float _meleeDamage = 100f;
    [SerializeField] private float _rangedDamage = 50f;

    [Header("Gathering")]
    [FormerlySerializedAs("gatherInteractionRadius")]
    [SerializeField] private float _gatherInteractionRadius = 1.5f;
    [FormerlySerializedAs("gatherDuration")]
    [SerializeField] private float _gatherDuration = 2f;
    [FormerlySerializedAs("boidRespawnDelay")]
    [SerializeField] private float _boidRespawnDelay = 5f;

    [Header("Interest points")]
    [Tooltip("Interest point prefab spawned inside the hunting ground.")]
    [FormerlySerializedAs("interestPointPrefab")]
    [SerializeField] private InterestPoint _interestPointPrefab;
    [FormerlySerializedAs("poiSpawnInterval")]
    [SerializeField] private float _interestPointInterval = 6f;
    [FormerlySerializedAs("maxActiveInterestPoints")]
    [SerializeField] private int _maxActiveInterestPoints = 5;
    [Header("Visual feedback")]
    [SerializeField] private Color _searchColor = Color.white;
    [SerializeField] private Color _attackColor = new Color(1f, 0.25f, 0.25f);
    [SerializeField] private Color _gatherColor = new Color(1f, 0.9f, 0.2f);
    [SerializeField] private Color _fleeColor = new Color(0.2f, 0.9f, 1f);
    [SerializeField] private Color _exitColor = new Color(0.3f, 1f, 0.4f);
    [SerializeField] private Color _deadColor = new Color(0.35f, 0.35f, 0.35f);

    private readonly FiniteStateMachine<HunterState> _fsm = new FiniteStateMachine<HunterState>();
    private readonly List<Zone> _safeZones = new List<Zone>();
    private readonly List<Node> _huntingNodes = new List<Node>();
    private readonly List<Renderer> _hiddenRenderers = new List<Renderer>();
    private readonly List<Collider> _disabledColliders = new List<Collider>();

    private ColorFeedback _colorFeedback;
    private Collider _bodyCollider;
    private GraphNavigator _navigator;
    private Graph _graph;

    private Zone _initialZone;
    private Zone _huntingZone;
    private Zone _exitZone;
    [SerializeField] private Vector3 _startPosition;

    private float _attackCooldown;
    private int _gathers;
    private bool _bodyVisible = true;
    private bool _captureRequested;
    private bool _hasReportedAvailability;
    private bool _reportedPresent;
    private bool _reportedSafe;

    private string _debugTarget = "-";
    private Zone _chosenSafeZone;

    public Zone InitialZone => _initialZone;
    public Zone HuntingZone => _huntingZone;
    public Zone ExitZone => _exitZone;
    public IReadOnlyList<Zone> SafeZones => _safeZones;

    public float MoveSpeed => _moveSpeed;
    public float FleeSpeed => _fleeSpeed;
    public float VisionRadius => _visionRadius;
    public float EyeHeight => _eyeHeight;
    public float TargetAcquireMargin => _targetAcquireMargin;
    public float GatherInteractionRadius => _gatherInteractionRadius;
    public float GatherDuration => _gatherDuration;
    public float BoidRespawnDelay => _boidRespawnDelay;
    public float InterestPointInterval => _interestPointInterval;
    public int MaxActiveInterestPoints => _maxActiveInterestPoints;
    public InterestPoint InterestPointPrefab => _interestPointPrefab;

    public int Gathers => _gathers;
    public bool IsDead => _fsm.CurrentStateID == HunterState.Dead;
    public bool IsPresent => _bodyVisible && !IsDead && !_captureRequested;
    public bool IsAttackReady => _attackCooldown <= 0f;
    public float AttackCooldownRemaining => Mathf.Max(0f, _attackCooldown);
    public HunterState CurrentState => _fsm.CurrentStateID;
    public Zone ChosenSafeZone => _chosenSafeZone;
    public float BodyRadius => _bodyCollider != null
        ? Mathf.Max(_bodyCollider.bounds.extents.x, _bodyCollider.bounds.extents.z) : 0.4f;
    public bool IsInSafeZone => Zone.IsInsideSafeZone(transform.position, BodyRadius);

    public HunterPerception Perception { get; private set; }
    public HunterInterestPoints InterestPoints { get; private set; }
    public int VisibleWolfCount => Perception != null ? Perception.VisibleWolfCount : 0;
    public bool ShouldFlee => IsPresent && !IsInSafeZone && Perception != null && Perception.CanSeeWolf;

    public string DebugTarget => _debugTarget;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        Current = null;
        AvailabilityChanged = delegate { };
    }

    private void Awake()
    {
        Current = this;
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        DisablePhysicsInterference();

        if (GetComponentInChildren<Collider>() == null)
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.55f;
        }

        GameLayers.SetLayer(gameObject, GameLayers.Hunter);
        _bodyCollider = GetComponent<Collider>();

        _colorFeedback = gameObject.GetOrAddComponent<ColorFeedback>();
        _colorFeedback.SetRenderer(GetComponentInChildren<Renderer>());

        _navigator = gameObject.GetOrAddComponent<GraphNavigator>();
        _navigator.Configure(false, true);
        FieldOfViewMultiOverlap sensor = gameObject.GetOrAddComponent<FieldOfViewMultiOverlap>();
        Perception = new HunterPerception(this, sensor);
        InterestPoints = new HunterInterestPoints(this);
        gameObject.GetOrAddComponent<PathVisual>();
    }

    private void Start()
    {
        _graph = Graph.Current;
        ResolveZones();
        if (_graph == null || _initialZone == null || _huntingZone == null || _exitZone == null || _safeZones.Count < 2)
        {
            Debug.LogWarning("The hunter needs a navigation graph and all Exam 2 zones.", this);
            enabled = false;
            return;
        }
        _startPosition = _initialZone.Center;
        _startPosition.y = transform.position.y;
        Perception.Configure();
        BuildHuntingNodeList();
        InitializeStates();
        _fsm.ChangeState(HunterState.Search);
        FeedbackLog.Add("[Hunter] Starting in the initial zone. Required gathers: " + RequiredGathers + " boids before reaching the exit.", new Color(0.4f, 1f, 0.5f));
    }

    private void ResolveZones()
    {
        _safeZones.Clear();
        foreach (Zone zone in Zone.All)
        {
            switch (zone.Type)
            {
                case ZoneType.Initial: _initialZone = zone; break;
                case ZoneType.Hunting: _huntingZone = zone; break;
                case ZoneType.Exit: _exitZone = zone; break;
                case ZoneType.Safe: _safeZones.Add(zone); break;
            }
        }
    }

    private void InitializeStates()
    {
        _fsm.AddGlobalTransition(HunterState.Dead, HasBeenCaptured);
        _fsm.AddState(HunterState.Search, new HunterSearchState(this));
        _fsm.AddState(HunterState.Attack, new HunterAttackState(this));
        _fsm.AddState(HunterState.Gather, new HunterGatherState(this));
        _fsm.AddState(HunterState.Flee, new HunterFleeState(this));
        _fsm.AddState(HunterState.Exit, new HunterExitState(this));
        _fsm.AddState(HunterState.Dead, new HunterDeadState(this));
        _fsm.OnStateChanged += ResetNavigation;
    }

    private bool HasBeenCaptured()
    {
        return _captureRequested;
    }

    private void ResetNavigation(HunterState state)
    {
        _navigator.ResetPath();
    }

    private void OnDestroy()
    {
        if (Current != this) return;
        Current = null;
        AvailabilityChanged(false, false);
    }

    private void BuildHuntingNodeList()
    {
        _huntingNodes.Clear();

        if (_graph == null || _huntingZone == null) return;

        foreach (Node node in _graph.GetAllNodes)
        {
            if (!node.IsSafeZone && _huntingZone.Contains(node.transform.position))
                _huntingNodes.Add(node);
        }
    }

    private void Update()
    {
        if (_attackCooldown > 0f) _attackCooldown -= Time.deltaTime;

        _fsm.Update();
    }

    private void LateUpdate()
    {
        bool present = IsPresent;
        bool safe = present && IsInSafeZone;
        if (_hasReportedAvailability && present == _reportedPresent && safe == _reportedSafe) return;

        _hasReportedAvailability = true;
        _reportedPresent = present;
        _reportedSafe = safe;
        AvailabilityChanged(present, safe);
    }

    public void MoveTo(Vector3 destination, float speed)
    {
        Vector3 next = _navigator.GetNextPoint(destination);
        MoveDirect(next, speed);
    }

    public void MoveDirect(Vector3 destination, float speed)
    {
        Vector3 direction = destination - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            StopMoving();
            return;
        }

        MoveWithSteering(destination, speed, _maxSteering, 0.6f);

        Quaternion desired = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, _turnSpeed * Time.deltaTime);
    }

    public void PursueBoid(BoidAgent target, float speed)
    {
        if (target == null) return;

        if (_graph != null && !_graph.IsPathClear(transform.position, target.transform.position))
        {
            MoveTo(target.transform.position, speed);
            return;
        }

        float distance = FlatDistanceTo(target.transform.position);
        float targetSpeed = target.Velocity.magnitude;
        float prediction = distance / Mathf.Max(speed + targetSpeed, 0.001f);
        Vector3 futurePosition = target.transform.position + target.Velocity * prediction;

        if (_graph != null && !_graph.IsPathClear(transform.position, futurePosition))
            futurePosition = target.transform.position;

        MoveDirect(futurePosition, speed);
    }

    public void StopMoving() => _velocity = Vector3.zero;



    public float FlatDistanceTo(Vector3 position)
    {
        Vector3 offset = position - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    public bool HasReached(Vector3 position, float arrivalDistance)
    {
        return FlatDistanceTo(position) <= arrivalDistance;
    }

    public Node GetRandomHuntingNode()
    {
        if (_huntingNodes.Count == 0) return null;
        return _huntingNodes[UnityEngine.Random.Range(0, _huntingNodes.Count)];
    }

    public void ResetAttackTimer()
    {
        _attackCooldown = TBA;
    }

    public void PerformMeleeAttack(BoidAgent target)
    {
        string action = "Melee attack on " + target.name;
        target.TakeDamage(_meleeDamage);
        if (_colorFeedback != null) _colorFeedback.Flash(_attackColor, 0.2f);
        FeedbackLog.Add("[Hunter] " + action, _attackColor);
        ResetAttackTimer();
    }

    public void PerformRangedAttack(BoidAgent target)
    {
        string action = "Ranged attack on " + target.name;
        target.TakeDamage(_rangedDamage);
        if (_colorFeedback != null) _colorFeedback.Flash(_attackColor, 0.2f);
        FeedbackLog.Add("[Hunter] " + action, _attackColor);
        ResetAttackTimer();
    }

    public void AddGather()
    {
        _gathers++;
        FeedbackLog.Add("[Hunter] Gather completed (" + _gathers + "/" + RequiredGathers + ").", new Color(0.4f, 1f, 0.4f));
    }

    public void ResetGathers() => _gathers = 0;

    public Zone ChooseNearestSafeZone()
    {
        Zone best = null;
        float bestCost = float.PositiveInfinity;

        foreach (Zone zone in _safeZones)
        {
            if (zone == null) continue;

            float cost = _graph != null
                ? _graph.GetPathCost(transform.position, zone.Center, false)
                : FlatDistanceTo(zone.Center);

            if (cost < bestCost)
            {
                bestCost = cost;
                best = zone;
            }
        }

        _chosenSafeZone = best;
        return best;
    }

    public void DieFromWolf()
    {
        if (IsDead || _captureRequested) return;

        FeedbackLog.Add("[Hunter] Captured by a wolf. Entering DEAD.", new Color(1f, 0.2f, 0.2f));
        _captureRequested = true;
    }

    public void SetBodyVisible(bool visible)
    {
        if (_bodyVisible == visible) return;

        _bodyVisible = visible;

        if (!visible)
        {
            _hiddenRenderers.Clear();
            _disabledColliders.Clear();

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled) continue;
                renderer.enabled = false;
                _hiddenRenderers.Add(renderer);
            }

            foreach (Collider collider in GetComponentsInChildren<Collider>())
            {
                if (!collider.enabled) continue;
                collider.enabled = false;
                _disabledColliders.Add(collider);
            }

            return;
        }

        foreach (Renderer renderer in _hiddenRenderers)
        {
            if (renderer != null) renderer.enabled = true;
        }

        foreach (Collider collider in _disabledColliders)
        {
            if (collider != null) collider.enabled = true;
        }

        _hiddenRenderers.Clear();
        _disabledColliders.Clear();
    }

    public void RespawnAtInitialZone()
    {
        _captureRequested = false;
        _chosenSafeZone = null;
        transform.position = _startPosition;
        transform.rotation = Quaternion.identity;
        ResetGathers();
        _attackCooldown = 0f;
        _navigator.ResetPath();
        SetBodyVisible(true);
        _debugTarget = "-";
    }

    public void SetFeedback(HunterState state, BoidAgent target = null)
    {
        _debugTarget = target != null ? target.name : "-";

        if (_colorFeedback != null)
        {
            _colorFeedback.SetColor(ColorFor(state));
        }
    }

    public Color ColorFor(HunterState state)
    {
        switch (state)
        {
            case HunterState.Attack: return _attackColor;
            case HunterState.Gather: return _gatherColor;
            case HunterState.Flee: return _fleeColor;
            case HunterState.Exit: return _exitColor;
            case HunterState.Dead: return _deadColor;
            default: return _searchColor;
        }
    }
}
