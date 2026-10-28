using System.Collections.Generic;
using UnityEngine;

public class BoidPerception
{
    private const float DetectionInterval = 0.2f;
    private readonly BoidAgent _owner;
    private readonly Collider[] _buffer = new Collider[128];
    private readonly List<BoidAgent> _neighbors = new List<BoidAgent>();
    private float _nextCheck;
    public IReadOnlyList<BoidAgent> Neighbors => _neighbors;
    public HunterAgent Threat { get; private set; }
    public InterestPoint InterestPoint { get; private set; }

    public BoidPerception(BoidAgent owner)
    {
        _owner = owner;
    }

    public void Refresh(float neighborRadius, float threatRadius, float interestRadius)
    {
        if (Time.time < _nextCheck) return;
        _nextCheck = Time.time + DetectionInterval;
        _neighbors.Clear();
        Threat = null;
        InterestPoint = null;
        float nearestThreat = threatRadius;
        float nearestInterest = interestRadius;
        Vector3 position = _owner.transform.position;
        float radius = Mathf.Max(neighborRadius, Mathf.Max(threatRadius, interestRadius));
        int mask = GameLayers.BoidMask | GameLayers.HunterMask | GameLayers.InterestMask;
        int count = Physics.OverlapSphereNonAlloc(position, radius, _buffer, mask);
        for (int i = 0; i < count; i++)
        {
            Collider detected = _buffer[i];
            float distance = Vector3.Distance(position, detected.transform.position);
            if (detected.gameObject.layer == GameLayers.Boid)
            {
                BoidAgent neighbor = detected.GetComponentInParent<BoidAgent>();
                if (neighbor != null && neighbor != _owner && distance <= neighborRadius && !_neighbors.Contains(neighbor))
                    _neighbors.Add(neighbor);
            }
            else if (detected.gameObject.layer == GameLayers.Hunter && distance <= nearestThreat)
            {
                HunterAgent hunter = detected.GetComponentInParent<HunterAgent>();
                if (hunter == null || !hunter.IsPresent) continue;
                Threat = hunter;
                nearestThreat = distance;
            }
            else if (detected.gameObject.layer == GameLayers.Interest && distance <= nearestInterest)
            {
                InterestPoint point = detected.GetComponentInParent<InterestPoint>();
                if (point == null || !point.isActiveAndEnabled) continue;
                if (Bounds.Instance != null && !Bounds.Instance.Contains(point.transform.position)) continue;
                InterestPoint = point;
                nearestInterest = distance;
            }
        }
    }

    public void Reset()
    {
        _nextCheck = 0f;
        _neighbors.Clear();
        Threat = null;
        InterestPoint = null;
    }
}
