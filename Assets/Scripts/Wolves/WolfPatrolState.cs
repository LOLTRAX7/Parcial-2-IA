using UnityEngine;

public class WolfPatrolState : State<WolfAgent.WolfState>
{
    private readonly WolfAgent _wolf;

    private int _index;
    private int _direction = 1;
    private bool _firstEnter = true;

    public WolfPatrolState(WolfAgent wolf)
    {
        _wolf = wolf;
    }

    public override void Enter()
    {
        if (_wolf.PatrolNodes.Count == 0) return;

        _index = _firstEnter ? 0 : GetClosestWaypointIndex();
        _firstEnter = false;
        FeedbackLog.Add(_wolf.name + " patrols through " + _wolf.PatrolNodes.Count + " waypoints.", new Color(0.75f, 0.75f, 0.8f), false);
    }

    public override void Update()
    {
        if (_wolf.PatrolNodes.Count == 0) return;

        if (_wolf.HunterInsideSafeZone || _wolf.HunterIsDead)
        {
            if (_wolf.HasKnownPosition) _wolf.ForgetHunter();
        }
        else if (_wolf.CanSeeHunter)
        {
            _fsm.ChangeState(WolfAgent.WolfState.Chase);
            return;
        }
        else if (_wolf.HasPendingAlert)
        {
            _fsm.ChangeState(WolfAgent.WolfState.GoToPosition);
            return;
        }

        Vector3 target = _wolf.PatrolNodes[_index].transform.position;

        _wolf.MoveTo(target, _wolf.PatrolSpeed);

        if (!_wolf.HasReached(target, 0.9f)) return;

        Advance();
    }

    private void Advance()
    {
        int count = _wolf.PatrolNodes.Count;

        if (count <= 1)
        {
            _index = 0;
            return;
        }

        if (!_wolf.PingPong)
        {
            _index = (_index + 1) % count;
            return;
        }

        _index += _direction;

        if (_index >= count)
        {
            _index = count - 2;
            _direction = -1;
        }
        else if (_index < 0)
        {
            _index = 1;
            _direction = 1;
        }
    }

    private int GetClosestWaypointIndex()
    {
        int best = 0;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < _wolf.PatrolNodes.Count; i++)
        {
            Vector3 offset = _wolf.PatrolNodes[i].transform.position - _wolf.transform.position;
            offset.y = 0f;

            if (offset.sqrMagnitude >= bestDistance) continue;

            bestDistance = offset.sqrMagnitude;
            best = i;
        }

        return best;
    }

    public override void Exit() => _wolf.StopMoving();
}
