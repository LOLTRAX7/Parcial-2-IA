using UnityEngine;

public class HunterPerception
{
    private readonly HunterAgent _hunter;
    private readonly FieldOfViewMultiOverlap _wolfSensor;
    private readonly Collider[] _queryBuffer = new Collider[128];

    public int VisibleWolfCount => _wolfSensor.VisibleTargets.Count;
    public bool CanSeeWolf => _wolfSensor.CanSeeTarget;

    public HunterPerception(HunterAgent hunter, FieldOfViewMultiOverlap wolfSensor)
    {
        _hunter = hunter;
        _wolfSensor = wolfSensor;
    }

    public void Configure()
    {
        _wolfSensor.Configure(_hunter.VisionRadius, 360f, _hunter.EyeHeight,
            GameLayers.WolfMask, GameLayers.WallMask);
    }

    public BoidAgent FindNearestLiveBoid()
    {
        return FindBestBoid(false, false);
    }

    public BoidAgent FindNearestEliminatedBoid()
    {
        return FindBestBoid(true, false);
    }

    public BoidAgent FindBestLiveBoid()
    {
        return FindBestBoid(false, true);
    }

    private BoidAgent FindBestBoid(bool eliminated, bool chooseWeakest)
    {
        BoidAgent best = null;
        float bestScore = float.PositiveInfinity;
        float bestDistance = float.PositiveInfinity;
        float searchRadius = _hunter.VisionRadius;
        if (!eliminated)
        {
            searchRadius = Mathf.Max(0f, _hunter.VisionRadius - _hunter.TargetAcquireMargin);
        }

        int count = Physics.OverlapSphereNonAlloc(_hunter.transform.position, _hunter.VisionRadius, _queryBuffer, GameLayers.BoidMask);

        for (int i = 0; i < count; i++)
        {
            BoidAgent boid = _queryBuffer[i].GetComponentInParent<BoidAgent>();

            if (boid == null) continue;
            if (boid.IsEliminated != eliminated) continue;
            if (eliminated && !boid.IsPendingGather) continue;

            float distance = _hunter.FlatDistanceTo(boid.transform.position);
            if (distance > searchRadius || !HasClearSightTo(boid.transform.position)) continue;

            float score = chooseWeakest ? boid.HealthNormalized : distance;

            if (score < bestScore - 0.001f || (Mathf.Abs(score - bestScore) <= 0.001f && distance < bestDistance))
            {
                best = boid;
                bestScore = score;
                bestDistance = distance;
            }
        }

        return best;
    }

    public bool HasClearSightTo(Vector3 targetPosition)
    {
        float height = _hunter.transform.position.y + _hunter.EyeHeight;
        Vector3 eye = new Vector3(_hunter.transform.position.x, height, _hunter.transform.position.z);
        Vector3 targetEye = new Vector3(targetPosition.x, height, targetPosition.z);

        return !Physics.Linecast(eye, targetEye, GameLayers.WallMask);
    }

}
