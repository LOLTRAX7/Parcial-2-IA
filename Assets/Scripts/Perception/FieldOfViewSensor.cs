using System;
using UnityEngine;

public abstract class FieldOfViewSensor : MonoBehaviour
{
    [Header("Field Of View")]
    [SerializeField] protected float _viewRadius = 8f;
    [SerializeField] protected float _eyeHeight = 1f;
    [SerializeField, Range(0f, 360f)] protected float _viewAngle = 90f;

    [Header("Layer filters")]
    [SerializeField] protected LayerMask _targetMask = ~0;
    [SerializeField] protected LayerMask _obstacleMask = ~0;

    protected Transform _target;

    public Func<Transform, bool> TargetFilter { get; set; }

    public bool CanSeeTarget { get; protected set; }

    public Transform Target => _target;
    public float ViewRadius => _viewRadius;
    public float ViewAngle => _viewAngle;
    public float EyeHeight => _eyeHeight;

    public void Configure(float viewRadius, float viewAngle, float eyeHeight, LayerMask targetMask, LayerMask obstacleMask)
    {
        _viewRadius = viewRadius;
        _viewAngle = viewAngle;
        _eyeHeight = eyeHeight;
        _targetMask = targetMask;
        _obstacleMask = obstacleMask;
    }

    protected bool IsAllowed(Transform candidate)
    {
        if (candidate == null) return false;
        return TargetFilter == null || TargetFilter(candidate);
    }

    protected bool InViewAngle()
    {
        if (_target == null) return false;
        if (_viewAngle >= 359f) return true;

        Vector3 dirToTarget = (_target.position - transform.position).normalized;
        return Vector3.Angle(transform.forward, dirToTarget) <= _viewAngle * 0.5f;
    }

    public bool IsVisibleNow(Transform candidate)
    {
        if (!IsAllowed(candidate)) return false;
        if ((candidate.position - transform.position).sqrMagnitude > _viewRadius * _viewRadius) return false;
        Transform previous = _target;
        _target = candidate;
        bool visible = InViewAngle() && HasLineOfSight();
        _target = previous;
        return visible;
    }

    protected bool HasLineOfSight()
    {
        if (_target == null) return false;

        Vector3 eyePos = transform.position + Vector3.up * _eyeHeight;
        Vector3 targetPos = _target.position + Vector3.up * _eyeHeight;
        Vector3 direction = targetPos - eyePos;

        if (Physics.Raycast(eyePos, direction.normalized, out RaycastHit hit, direction.magnitude, _obstacleMask))
            return hit.transform == _target || hit.transform.IsChildOf(_target);

        return true;
    }
}
