using UnityEngine;

public class HunterGatherState : State<HunterAgent.HunterState>
{
    private readonly HunterAgent _hunter;
    private BoidAgent _target;
    private float _gatherTimer;

    public HunterGatherState(HunterAgent hunter)
    {
        _hunter = hunter;
    }

    public override void Enter()
    {
        _target = _hunter.Perception.FindNearestEliminatedBoid();
        _gatherTimer = 0f;

        if (_target == null)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Search);
            return;
        }

        _hunter.SetFeedback(HunterAgent.HunterState.Gather, _target);
        FeedbackLog.Add("[Hunter] Gather: moving to collect " + _target.name + ".", new Color(1f, 0.9f, 0.3f), false);
    }

    public override void Update()
    {
        if (_hunter.ShouldFlee)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Flee);
            return;
        }

        if (_target == null || !_target.isActiveAndEnabled || !_target.IsEliminated || !_target.IsPendingGather)
        {
            FeedbackLog.Add("[Hunter] Gather cancelled: target no longer available.", Color.yellow, false);
            _fsm.ChangeState(HunterAgent.HunterState.Search);
            return;
        }

        float distance = _hunter.FlatDistanceTo(_target.transform.position);

        if (distance > _hunter.GatherInteractionRadius)
        {
            _gatherTimer = 0f;
            if (_hunter.Perception.HasClearSightTo(_target.transform.position))
                _hunter.MoveDirect(_target.transform.position, _hunter.MoveSpeed);
            else
                _hunter.MoveTo(_target.transform.position, _hunter.MoveSpeed);

            _hunter.SetFeedback(HunterAgent.HunterState.Gather, _target);
            return;
        }

        _hunter.StopMoving();
        _gatherTimer += Time.deltaTime;
        _hunter.SetFeedback(HunterAgent.HunterState.Gather, _target);

        if (_gatherTimer < _hunter.GatherDuration) return;

        _target.OnGathered(_hunter.BoidRespawnDelay);
        _hunter.AddGather();

        if (_hunter.Gathers >= _hunter.RequiredGathers && _hunter.ExitZone != null)
            _fsm.ChangeState(HunterAgent.HunterState.Exit);
        else
            _fsm.ChangeState(HunterAgent.HunterState.Search);
    }

    public override void Exit()
    {
        _hunter.StopMoving();
    }
}
