using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    [SerializeField] private Renderer _renderer;
    [SerializeField] private bool _blocked;
    [SerializeField] private bool _isSafeZone;
    [SerializeField] private Color _defaultColor = new Color(1f, 0.25f, 0.65f);

    private readonly List<Node> _neighbors = new List<Node>();

    public IReadOnlyList<Node> Neighbors => _neighbors;

    public bool Blocked => _blocked;

    public bool IsSafeZone => _isSafeZone;

    public void AddNeighbor(Node node)
    {
        if (node == null || node == this || _neighbors.Contains(node)) return;

        _neighbors.Add(node);
    }

    public void ClearNeighbors() => _neighbors.Clear();

    public void SetSafeZone(bool value) => _isSafeZone = value;

    public float DistanceTo(Node other) => Vector3.Distance(transform.position, other.transform.position);

    public void SetMarkerRenderer(Renderer renderer)
    {
        _renderer = renderer;
        ResetColor();
    }

    public void SetDefaultColor(Color color)
    {
        _defaultColor = color;
        ResetColor();
    }

    public void ChangeColor(Color color)
    {
        if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null) _renderer.material.color = color;
    }

    public void ResetColor() => ChangeColor(_blocked ? Color.red : _defaultColor);

    private void OnValidate()
    {
        if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _isSafeZone ? new Color(0.65f, 0.5f, 0.92f) : _defaultColor;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.15f, 0.28f);
    }
}
