using UnityEngine;

public class HunterFleeState : State<HunterAgent.HunterState>
{
    private readonly HunterAgent _hunter;
    private Zone _safeZone;
    private float _waitTimer;
    private bool _waiting;

    public HunterFleeState(HunterAgent hunter)
    {
        _hunter = hunter;
    }

    public override void Enter()
    {
        _waitTimer = 0f;
        _waiting = false;

        _safeZone = _hunter.ChooseNearestSafeZone();

        if (_safeZone == null)
        {
            FeedbackLog.Add("[Hunter] Flee: no safe zone assigned.", Color.red);
            _fsm.ChangeState(HunterAgent.HunterState.Search);
            return;
        }

        string safeName = _safeZone.name;
        _hunter.SetFeedback(HunterAgent.HunterState.Flee, null);
        FeedbackLog.Add("[Hunter] Wolf spotted. Fleeing to the nearest safe zone by A*: " + safeName + ".", new Color(0.2f, 0.9f, 1f));
    }

    public override void Update()
    {
        if (_safeZone == null)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Search);
            return;
        }

        if (_hunter.IsInSafeZone && (_waiting || _hunter.HasReached(_safeZone.Center, 0.6f)))
        {
            _hunter.StopMoving();

            if (!_waiting)
            {
                _waiting = true;
                _waitTimer = _hunter.SafeZoneWaitTime;
                _hunter.SetFeedback(HunterAgent.HunterState.Flee, null);
                FeedbackLog.Add("[Hunter] Entered " + _safeZone.name + ". Wolves lose their target. Waiting " + _waitTimer.ToString("0.0") + " s.", new Color(0.2f, 0.9f, 1f), false);
            }

            _waitTimer -= Time.deltaTime;

            if (_waitTimer > 0f) return;

            FeedbackLog.Add("[Hunter] Safe zone wait finished. Resuming activity.", new Color(0.2f, 0.9f, 1f), false);

            if (_hunter.Gathers >= _hunter.RequiredGathers)
                _fsm.ChangeState(HunterAgent.HunterState.Exit);
            else
                _fsm.ChangeState(HunterAgent.HunterState.Search);

            return;
        }

        _hunter.MoveTo(_safeZone.Center, _hunter.FleeSpeed);

        _hunter.SetFeedback(HunterAgent.HunterState.Flee, null);
    }

    public override void Exit()
    {
        _hunter.StopMoving();
    }
}
