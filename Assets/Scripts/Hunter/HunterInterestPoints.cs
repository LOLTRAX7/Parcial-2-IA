using UnityEngine;

public class HunterInterestPoints
{
    private readonly HunterAgent _hunter;

    public HunterInterestPoints(HunterAgent hunter)
    {
        _hunter = hunter;
    }

    public bool HasRoom()
    {
        if (_hunter.InterestPointPrefab == null) return false;

        int activeCount = Object.FindObjectsByType<InterestPoint>(FindObjectsSortMode.None).Length;
        return activeCount < _hunter.MaxActiveInterestPoints;
    }

    public void SpawnInHuntingZone()
    {
        Zone zone = _hunter.HuntingZone;
        if (zone == null || !zone.Contains(_hunter.transform.position) || !HasRoom()) return;

        Spawn(zone.RandomPoint());
    }

    public void SpawnAtHunter()
    {
        if (!HasRoom()) return;
        Spawn(_hunter.transform.position);
    }

    private void Spawn(Vector3 position)
    {
        position.y = 0.35f;
        Object.Instantiate(_hunter.InterestPointPrefab, position, Quaternion.identity);
        FeedbackLog.Add("[Hunter] Placing an interest point at " + position + ".", Color.yellow, false);
    }
}
