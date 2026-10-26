using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-300)]
public class Graph : MonoBehaviour
{
    [Header("Connections")]
    [Tooltip("Maximum distance between connected nodes.")]
    [SerializeField] private float _maxLinkDistance = 24f;
    [Tooltip("Sphere radius for validating graph links; smaller than the doorway width.")]
    [SerializeField] private float _linkRadius = 0.65f;
    [Tooltip("Agent body radius for direct movement checks.")]
    [SerializeField] private float _agentRadius = 0.45f;
    [SerializeField] private float _castHeight = 0.5f;
    [Tooltip("Layers treated as walls.")]
    [SerializeField] private LayerMask _wallMask;

    [Header("Debug")]
    [Tooltip("Draw graph edges in the Game view with LineRenderers.")]
    [SerializeField] private bool _drawEdgesInGame = false;
    [SerializeField] private Color _edgeColor = new Color(1f, 0.45f, 0.8f, 0.65f);

    private readonly List<Node> _nodes = new List<Node>();
    private Transform _edgeVisuals;
    private bool _generated;

    public static Graph Current { get; private set; }

    public List<Node> GetAllNodes => _nodes;
    public LayerMask WallMask => _wallMask;
    public float CastHeight => _castHeight;
    public bool IsGenerated => _generated;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() => Current = null;

    private void Awake()
    {
        if (Current != null && Current != this)
        {
            Destroy(gameObject);
            return;
        }

        Current = this;
        _nodes.AddRange(GetComponentsInChildren<Node>());
    }

    private void Start()
    {
        if (!_generated) GenerateConnections();
    }

    public void Configure(LayerMask wallMask)
    {
        _wallMask = wallMask;
    }

    public IReadOnlyList<Node> GetNeighbors(Node current) => current.Neighbors;

    public void GenerateConnections()
    {
        Physics.SyncTransforms();

        foreach (Node node in _nodes) node.ClearNeighbors();

        float maxSqr = _maxLinkDistance * _maxLinkDistance;

        for (int i = 0; i < _nodes.Count; i++)
        {
            for (int j = i + 1; j < _nodes.Count; j++)
            {
                Node a = _nodes[i];
                Node b = _nodes[j];

                if (FlatSqrDistance(a.transform.position, b.transform.position) > maxSqr) continue;
                if (!IsLinkClear(a.transform.position, b.transform.position)) continue;

                a.AddNeighbor(b);
                b.AddNeighbor(a);
            }
        }

        _generated = true;
        BuildEdgeVisuals();
    }

    public bool IsPathClear(Vector3 origin, Vector3 destination)
    {
        return SweepClear(origin, destination, _agentRadius);
    }

    public bool CanTravel(Vector3 from, Vector3 to, bool avoidSafeZones = false)
    {
        return (!avoidSafeZones || !Zone.CrossesSafeZone(from, to, _agentRadius))
            && IsPathClear(from, to);
    }

    public bool IsLinkClear(Vector3 a, Vector3 b)
    {
        return SweepClear(a, b, _linkRadius);
    }

    private bool SweepClear(Vector3 origin, Vector3 destination, float radius)
    {
        Vector3 start = new Vector3(origin.x, _castHeight, origin.z);
        Vector3 end = new Vector3(destination.x, _castHeight, destination.z);

        Vector3 direction = end - start;
        float distance = direction.magnitude;

        if (Physics.CheckSphere(start, radius, _wallMask, QueryTriggerInteraction.Ignore)
            || Physics.CheckSphere(end, radius, _wallMask, QueryTriggerInteraction.Ignore)) return false;

        if (distance < 0.001f) return true;

        Vector3 unit = direction / distance;

        if (Physics.SphereCast(start, radius, unit, out RaycastHit _, distance, _wallMask)) return false;
        if (Physics.SphereCast(end, radius, -unit, out RaycastHit _, distance, _wallMask)) return false;

        return true;
    }

    public Node GetClosestNode(Vector3 position, bool avoidSafeZones = false)
    {
        Node best = null;
        float bestSqr = float.MaxValue;

        foreach (Node node in _nodes)
        {
            if (node.Blocked) continue;
            if (avoidSafeZones && node.IsSafeZone) continue;

            float sqr = FlatSqrDistance(position, node.transform.position);

            if (sqr >= bestSqr) continue;

            best = node;
            bestSqr = sqr;
        }

        return best;
    }

    public Node GetClosestVisibleNode(Vector3 position, bool strict, bool avoidSafeZones = false)
    {
        Node best = null;
        float bestSqr = float.MaxValue;

        foreach (Node node in _nodes)
        {
            if (node.Blocked) continue;
            if (avoidSafeZones && node.IsSafeZone) continue;

            float sqr = FlatSqrDistance(position, node.transform.position);

            if (sqr >= bestSqr) continue;

            if (avoidSafeZones && Zone.CrossesSafeZone(position, node.transform.position, _agentRadius)) continue;

            bool visible = strict
                ? IsLinkClear(position, node.transform.position)
                : IsPathClear(position, node.transform.position);

            if (!visible) continue;

            best = node;
            bestSqr = sqr;
        }

        return best;
    }

    public void GetNodesWithin(Vector3 center, float radius, List<Node> results, bool avoidSafeZones = false)
    {
        results.Clear();

        float sqr = radius * radius;

        foreach (Node node in _nodes)
        {
            if (node.Blocked) continue;
            if (avoidSafeZones && node.IsSafeZone) continue;

            if (FlatSqrDistance(center, node.transform.position) <= sqr) results.Add(node);
        }
    }

    public float GetPathCost(Vector3 from, Vector3 to, bool avoidSafeZones = false)
    {
        Node start = GetClosestVisibleNode(from, false, avoidSafeZones);
        Node goal = GetClosestVisibleNode(to, true, avoidSafeZones);

        if (goal == null) goal = GetClosestVisibleNode(to, false, avoidSafeZones);
        if (start == null || goal == null) return float.PositiveInfinity;

        List<Node> path = Pathfinding.AStar(start, goal, avoidSafeZones);

        if (path.Count == 0) return float.PositiveInfinity;

        float cost = FlatDistance(from, start.transform.position) + FlatDistance(goal.transform.position, to);

        for (int i = 0; i < path.Count - 1; i++)
        {
            cost += path[i].DistanceTo(path[i + 1]);
        }

        return cost;
    }

    private static float FlatSqrDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        return Mathf.Sqrt(FlatSqrDistance(a, b));
    }

    private void BuildEdgeVisuals()
    {
        if (_edgeVisuals != null) Destroy(_edgeVisuals.gameObject);

        if (!_drawEdgesInGame) return;

        _edgeVisuals = new GameObject("Edges").transform;
        _edgeVisuals.SetParent(transform, false);

        Material material = FeedbackMaterials.CreateUnlit(Color.white);
        Vector3 lift = Vector3.up * 0.08f;

        for (int i = 0; i < _nodes.Count; i++)
        {
            foreach (Node other in _nodes[i].Neighbors)
            {
                if (_nodes.IndexOf(other) < i) continue;

                GameObject edge = new GameObject("Edge");
                edge.transform.SetParent(_edgeVisuals, false);

                LineRenderer line = edge.AddComponent<LineRenderer>();
                line.sharedMaterial = material;
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.SetPosition(0, _nodes[i].transform.position + lift);
                line.SetPosition(1, other.transform.position + lift);
                line.widthMultiplier = 0.06f;
                line.startColor = _edgeColor;
                line.endColor = _edgeColor;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.45f, 0.8f, 0.55f);
        Vector3 lift = Vector3.up * 0.15f;

        foreach (Node node in _nodes)
        {
            if (node == null) continue;

            foreach (Node other in node.Neighbors)
            {
                if (other == null) continue;

                Gizmos.DrawLine(node.transform.position + lift, other.transform.position + lift);
            }
        }
    }
}
