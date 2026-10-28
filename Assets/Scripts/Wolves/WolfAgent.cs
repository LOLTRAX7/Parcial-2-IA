using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(FieldOfViewOverlap))]
[RequireComponent(typeof(GraphNavigator))]
[RequireComponent(typeof(ColorFeedback))]
[RequireComponent(typeof(Collider))]
public class WolfAgent : Agent
{
    public enum WolfState { Patrol, Chase, GoToPosition, Search }

    public event Action<WolfState> OnStateChanged = delegate { };
    public event Action<Vector3> OnHowled = delegate { };
    public event Action<WolfAgent, Vector3> OnAlertReceived = delegate { };

    public static event Action<WolfAgent, Vector3> AlertIssued = delegate { };

    [Header("Speeds (lower than the hunter)")]
    [SerializeField] private float _patrolSpeed = 3.8f;
    [SerializeField] private float _alertSpeed = 4.6f;
    [SerializeField] private float _chaseSpeed = 5.1f;
    [SerializeField] private float _maxSteering = 20f;

    [Header("Perception (FoV)")]
    [SerializeField] private float _viewRadius = 18f;
    [SerializeField, Range(0f, 360f)] private float _viewAngle = 110f;
    [SerializeField] private float _eyeHeight = 0.6f;

    [Header("Capture")]
    [SerializeField] private float _captureRadius = 1.3f;

    [Header("Alert and howl")]
    [SerializeField] private float _howlCooldown = 6f;

    [Header("Search")]
    [Tooltip("Search node refresh interval. Search ends on detection, an alert, or when the hunter enters safety or dies.")]
    [SerializeField] private float _searchDuration = 6f;
    [SerializeField] private float _scanDuration = 1f;
    [SerializeField] private float _searchRadius = 14f;

    private readonly FiniteStateMachine<WolfState> _fsm = new FiniteStateMachine<WolfState>();
    [SerializeField] private List<Node> _patrolNodes = new List<Node>();

    private FieldOfViewOverlap _sensor;
    private GraphNavigator _navigator;
    private ColorFeedback _colorFeedback;
    private Graph _graph;

    [SerializeField] private bool _pingPong = true;
    private bool _configured;
    private bool _hasKnownPosition;
    private bool _hasPendingAlert;
    private Vector3 _lastKnownPosition;
    private Vector3 _pendingAlertPosition;
    private float _nextHowlTime;
    private bool _loggedNoRoute;
    private bool _hunterPresent = true;
    private bool _hunterProtected;

    public WolfState CurrentState => _fsm.CurrentStateID;

    public IReadOnlyList<Node> PatrolNodes => _patrolNodes;
    public bool PingPong => _pingPong;

    public float PatrolSpeed => _patrolSpeed;
    public float AlertSpeed => _alertSpeed;
    public float ChaseSpeed => _chaseSpeed;
    public float CaptureRadius => _captureRadius;
    public float ViewRadius => _viewRadius;
    public float SearchDurationForState => _searchDuration;
    public float ScanDuration => _scanDuration;
    public float SearchRadius => _searchRadius;

    public FieldOfViewOverlap Sensor => _sensor;
    public GraphNavigator Navigator => _navigator;
    public Graph Graph => _graph;

    public bool HasKnownPosition => _hasKnownPosition;
    public bool HasPendingAlert => _hasPendingAlert;
    public Vector3 LastKnownPosition => _lastKnownPosition;
    public HunterAgent Hunter
    {
        get
        {
            Transform target = _sensor != null ? _sensor.Target : null;
            return target != null ? target.GetComponentInParent<HunterAgent>() : null;
        }
    }

    public bool CanSeeHunter
    {
        get
        {
            HunterAgent hunter = Hunter;
            return !_hunterProtected && _hunterPresent && _sensor != null && _sensor.CanSeeTarget
                && hunter != null && hunter.IsPresent && !hunter.IsInSafeZone
                && _sensor.IsVisibleNow(hunter.transform);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        AlertIssued = delegate { };
    }

    private void Awake()
    {
        DisablePhysicsInterference();
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        if (GetComponentInChildren<Collider>() == null)
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.5f;
        }

        GameLayers.SetLayer(gameObject, GameLayers.Wolf);

        _colorFeedback = gameObject.GetOrAddComponent<ColorFeedback>();
        _colorFeedback.SetRenderer(GetComponentInChildren<Renderer>());

        _navigator = gameObject.GetOrAddComponent<GraphNavigator>();
        _navigator.Configure(true);

        _sensor = gameObject.GetOrAddComponent<FieldOfViewOverlap>();

        _sensor.TargetFilter = IsHunterDetectable;

        gameObject.GetOrAddComponent<FieldOfViewGizmos>();
        gameObject.GetOrAddComponent<PathVisual>();
        gameObject.GetOrAddComponent<WolfFeedback>();
    }

    private bool IsHunterDetectable(Transform candidate)
    {
        if (candidate == null) return false;

        HunterAgent hunter = candidate.GetComponentInParent<HunterAgent>();
        if (hunter == null) return false;

        return hunter.IsPresent && !hunter.IsInSafeZone;
    }

    private void OnEnable()
    {
        AlertIssued += HandleAlertIssued;
        HunterAgent.AvailabilityChanged += HandleHunterAvailability;
    }

    private void OnDisable()
    {
        AlertIssued -= HandleAlertIssued;
        HunterAgent.AvailabilityChanged -= HandleHunterAvailability;
    }

    private void Start()
    {
        InitializeNavigation();
        InitializeStates();
    }

    private void InitializeNavigation()
    {
        _graph = Graph.Current;
        _configured = _patrolNodes.Count > 0;

        _sensor.Configure(_viewRadius, _viewAngle, _eyeHeight, GameLayers.HunterMask, GameLayers.WallMask);
        _navigator.Configure(true);

    }

    private void InitializeStates()
    {
        _fsm.AddState(WolfState.Patrol, new WolfPatrolState(this));
        _fsm.AddState(WolfState.Chase, new WolfChaseState(this));
        _fsm.AddState(WolfState.GoToPosition, new WolfGoToPositionState(this));
        _fsm.AddState(WolfState.Search, new WolfSearchState(this));

        _fsm.OnStateChanged += HandleStateChanged;
        _fsm.ChangeState(WolfState.Patrol);
    }

    private void Update()
    {
        if (!_configured)
        {
            if (!_loggedNoRoute)
            {
                _loggedNoRoute = true;
                FeedbackLog.Add(name + " has no patrol route assigned.", Color.red);
            }

            return;
        }

        _fsm.Update();
    }

    private void HandleStateChanged(WolfState state)
    {
        _navigator.Configure(true, state == WolfState.GoToPosition);
        _navigator.ResetPath();
        OnStateChanged(state);
    }

    private void HandleHunterAvailability(bool present, bool safe)
    {
        _hunterPresent = present;
        _hunterProtected = safe;
        if (!present || safe) ForgetHunter();
    }

    public void Configure(Graph graph, IReadOnlyList<Node> patrolNodes, bool pingPong)
    {
        _graph = graph;
        _pingPong = pingPong;

        _patrolNodes.Clear();

        for (int i = 0; i < patrolNodes.Count; i++)
        {
            if (patrolNodes[i] != null) _patrolNodes.Add(patrolNodes[i]);
        }

        _configured = _patrolNodes.Count > 0;
    }

    public void MoveTo(Vector3 destination, float speed)
    {
        if (_navigator.IsDestinationForbidden(destination))
        {
            StopMoving();
            return;
        }

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

        MoveWithSteering(destination, speed, _maxSteering, 0.6f, true);
        if (_velocity.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(_velocity.normalized, Vector3.up);
    }

    public void StopMoving()
    {
        _velocity = Vector3.zero;
    }

    public bool HasReached(Vector3 position, float arrivalDistance)
    {
        Vector3 offset = position - transform.position;
        offset.y = 0f;
        return offset.magnitude <= arrivalDistance;
    }

    public float FlatDistanceTo(Vector3 position)
    {
        Vector3 offset = position - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    public float FlatDistanceToHunter()
    {
        return CanSeeHunter ? FlatDistanceTo(Hunter.transform.position) : float.PositiveInfinity;
    }

    public void Scan(float degreesPerSecond)
    {
        transform.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime, Space.World);
    }

    public void NotifyTargetSpotted()
    {
        HunterAgent hunter = Hunter;

        if (hunter == null || !CanSeeHunter)
        {
            ForgetHunter();
            return;
        }

        _lastKnownPosition = hunter.transform.position;
        _hasKnownPosition = true;
        _hasPendingAlert = false;

        if (Time.time < _nextHowlTime) return;

        _nextHowlTime = Time.time + _howlCooldown;

        OnHowled(transform.position);
        AlertIssued(this, _lastKnownPosition);

        FeedbackLog.Add(name + " howls: hunter spotted at " + Format(_lastKnownPosition) + ".", new Color(1f, 0.6f, 0.1f));
    }

    private void HandleAlertIssued(WolfAgent source, Vector3 position)
    {
        if (source == this) return;
        if (!_hunterPresent || _hunterProtected) return;

        _pendingAlertPosition = position;
        _hasPendingAlert = true;

        OnAlertReceived(source, position);
        FeedbackLog.Add(name + " receives an alert from " + source.name + ": position " + Format(position) + ".", new Color(0.3f, 0.9f, 1f), false);
    }

    public void ConsumePendingAlert()
    {
        if (!_hasPendingAlert) return;

        _lastKnownPosition = _pendingAlertPosition;
        _hasKnownPosition = true;
        _hasPendingAlert = false;
    }

    public void ForgetHunter()
    {
        _hasKnownPosition = false;
        _hasPendingAlert = false;
        _lastKnownPosition = Vector3.zero;
        _pendingAlertPosition = Vector3.zero;

        if (_navigator != null) _navigator.ResetPath();
    }

    public bool HunterInsideSafeZone => _hunterProtected;
    public bool HunterIsDead => !_hunterPresent;

    public bool TryCaptureHunter()
    {
        if (!CanSeeHunter) return false;
        HunterAgent hunter = Hunter;

        if (hunter == null || !hunter.IsPresent || hunter.IsInSafeZone) return false;
        if (FlatDistanceToHunter() > _captureRadius) return false;
        if (_graph != null && !_graph.IsPathClear(transform.position, hunter.transform.position)) return false;

        FeedbackLog.Add(name + " reached the hunter inside CaptureRadius (" + _captureRadius.ToString("0.0") + " m).", new Color(1f, 0.15f, 0.15f));

        hunter.DieFromWolf();
        ForgetHunter();
        return true;
    }

    public static string Format(Vector3 position)
    {
        return "(" + position.x.ToString("0.0") + ", " + position.z.ToString("0.0") + ")";
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, _captureRadius);

        if (_hasKnownPosition)
        {
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.9f);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.2f, _lastKnownPosition + Vector3.up * 0.2f);
            Gizmos.DrawWireSphere(_lastKnownPosition, 0.45f);
        }
    }
}
