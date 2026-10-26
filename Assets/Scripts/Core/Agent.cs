using UnityEngine;

public class Agent : MonoBehaviour
{
    protected Vector3 _velocity;

    public Vector3 Velocity => _velocity;

    protected void MoveWithSteering(Vector3 target, float speed, float maxSteering,
        float slowingDistance, bool avoidSafeZones = false)
    {
        Vector3 offset = target - transform.position;
        offset.y = 0f;
        float distance = offset.magnitude;
        if (distance < 0.02f)
        {
            _velocity = Vector3.zero;
            return;
        }

        float desiredSpeed = speed * Mathf.Clamp01(distance / Mathf.Max(slowingDistance, 0.02f));
        Vector3 desired = offset / distance * desiredSpeed;
        _velocity += Vector3.ClampMagnitude(desired - _velocity, maxSteering * Time.deltaTime);
        _velocity = Vector3.ClampMagnitude(_velocity, speed);
        Vector3 step = _velocity * Time.deltaTime;
        if (step.magnitude > distance) step = step.normalized * distance;

        Graph graph = Graph.Current;
        if (graph != null && !graph.CanTravel(transform.position, transform.position + step, avoidSafeZones))
        {
            _velocity = Vector3.zero;
            return;
        }

        transform.position += step;
    }

    protected void DisablePhysicsInterference()
    {
        if (TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }
}
