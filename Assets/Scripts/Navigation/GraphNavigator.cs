using System.Collections.Generic;
using UnityEngine;

public class GraphNavigator : MonoBehaviour
{
    [SerializeField] private float _nodeArrivalDistance = 0.5f;
    [SerializeField] private float _destinationRepathDistance = 2f;
    [SerializeField] private float _minRepathInterval = 0.3f;
    [SerializeField] private float _lineCheckInterval = 0.25f;


    private readonly List<Vector3> _path = new List<Vector3>();

    private Vector3 _plannedDestination;

    private bool _hasPlan;
    private bool _avoidSafeZones;
    private bool _requireGraphPath;
    private bool _routeFound;
    private float _lastPlanTime = -999f;
    private float _nextLineCheck;
    public bool UsingPathfinding => _path.Count > 0;
    public IReadOnlyList<Vector3> PathPoints => _path;
    public Vector3 CurrentDestination => _plannedDestination;
    public bool AvoidSafeZones => _avoidSafeZones;

    public void Configure(bool avoidSafeZones, bool requireGraphPath = false)
    {
        _avoidSafeZones = avoidSafeZones;
        _requireGraphPath = requireGraphPath;

    }
    public void ResetPath()
    {
        _path.Clear();
        _hasPlan = false;
        _routeFound = false;

    }
    public bool IsDestinationForbidden(Vector3 destination)
    {
        return _avoidSafeZones && Zone.IsInsideSafeZone(destination);
    }
    public Vector3 GetNextPoint(Vector3 destination)
    {
        Graph graph = Graph.Current;

        if (graph == null) return transform.position;
        if (IsDestinationForbidden(destination))
        {
            _path.Clear();
            return transform.position;
        }

        Vector3 position = transform.position;
        bool destinationMoved = Flat(destination - _plannedDestination).sqrMagnitude > _destinationRepathDistance * _destinationRepathDistance;

        if (!_hasPlan || ((destinationMoved || !_routeFound) && Time.time - _lastPlanTime >= _minRepathInterval))
        {
            Plan(graph, position, destination);
        }

        else if (Time.time >= _nextLineCheck)
        {
            _nextLineCheck = Time.time + _lineCheckInterval;

            bool clear = graph.CanTravel(position, destination, _avoidSafeZones);

            if (_path.Count == 0 && !clear) Plan(graph, position, destination);
            else if (!_requireGraphPath && _path.Count > 0 && clear) _path.Clear();
        }

        while (_path.Count > 0 && Flat(_path[0] - position).magnitude <= _nodeArrivalDistance)
        {
            Vector3 following = _path.Count > 1 ? _path[1] : destination;
            if (!graph.CanTravel(position, following, _avoidSafeZones)) break;
            _path.RemoveAt(0);
        }

        if (!_routeFound) return position;
        if (_path.Count > 0) return _path[0];
        return graph.CanTravel(position, destination, _avoidSafeZones) ? destination : position;
    }
    private void Plan(Graph graph, Vector3 position, Vector3 destination)
    {
        _hasPlan = true;
        _plannedDestination = destination;
        _lastPlanTime = Time.time;
        _nextLineCheck = Time.time + _lineCheckInterval;
        _path.Clear();
        _routeFound = false;

        if (!_requireGraphPath && graph.CanTravel(position, destination, _avoidSafeZones))
        {
            _routeFound = true;
            return;
        }
        Node start = graph.GetClosestVisibleNode(position, false, _avoidSafeZones);
        Node goal = graph.GetClosestVisibleNode(destination, true, _avoidSafeZones);

        if (goal == null) goal = graph.GetClosestVisibleNode(destination, false, _avoidSafeZones);
        if (start == null || goal == null) return;

        List<Node> nodes = Pathfinding.AStar(start, goal, _avoidSafeZones);

        if (nodes.Count == 0) return;
        _routeFound = true;
        foreach (Node node in nodes) _path.Add(node.transform.position);

        SmoothPath(graph, position, destination);

    }
    // Salta nodos si el tramo queda libre para pasar 
    private void SmoothPath(Graph graph, Vector3 position, Vector3 destination)
    {
        
        while (_path.Count > 1 && graph.CanTravel(position, _path[1], _avoidSafeZones))
        {
            _path.RemoveAt(0);
        }
        while (_path.Count > 1 && graph.CanTravel(_path[_path.Count - 2], destination, _avoidSafeZones))
        {
            _path.RemoveAt(_path.Count - 1);
        }
    }
    private static Vector3 Flat(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }
}
