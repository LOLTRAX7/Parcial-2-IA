using UnityEngine;

[RequireComponent(typeof(GraphNavigator))]
public class PathVisual : MonoBehaviour
{
    [SerializeField] private Color _color = new Color(1f, 0.6f, 0.1f);
    [SerializeField] private float _height = 0.2f;
    private GraphNavigator _navigator;

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (_navigator == null) _navigator = GetComponent<GraphNavigator>();
        if (!_navigator.UsingPathfinding) return;

        Gizmos.color = _color;
        Vector3 previous = Lift(transform.position);
        foreach (Vector3 point in _navigator.PathPoints)
        {
            Vector3 next = Lift(point);
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
        Gizmos.DrawLine(previous, Lift(_navigator.CurrentDestination));
    }

    private Vector3 Lift(Vector3 point)
    {
        return new Vector3(point.x, _height, point.z);
    }
}
