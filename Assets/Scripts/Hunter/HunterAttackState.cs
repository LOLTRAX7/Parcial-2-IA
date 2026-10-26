using UnityEngine;

public class HunterAttackState : State<HunterAgent.HunterState>
{
    private readonly HunterAgent _hunter;
    private BoidAgent _target;

    public HunterAttackState(HunterAgent hunter)
    {
        _hunter = hunter;
    }

    public override void Enter()
    {
        _target = _hunter.Perception.FindBestLiveBoid();

        if (_target == null)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Search);
            return;
        }

        _hunter.SetFeedback(HunterAgent.HunterState.Attack, _target);
        FeedbackLog.Add("[Hunter] Attack: target " + _target.name + ".", new Color(1f, 0.4f, 0.4f), false);
    }

    public override void Update()
    {
        if (_hunter.ShouldFlee)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Flee);
            return;
        }

        if (_hunter.Perception.FindNearestEliminatedBoid() != null)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Gather);
            return;
        }

        if (_target == null || !_target.isActiveAndEnabled || _target.IsEliminated)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Search);
            return;
        }

        float distance = _hunter.FlatDistanceTo(_target.transform.position);
        bool inSight = distance <= _hunter.VisionRadius && _hunter.Perception.HasClearSightTo(_target.transform.position);

        if (!inSight)
        {
            _hunter.SetFeedback(HunterAgent.HunterState.Attack, _target);
            _fsm.ChangeState(HunterAgent.HunterState.Search);
            return;
        }

        if (distance <= _hunter.MeleeAttackRadius)
        {
            _hunter.StopMoving();
            _hunter.PerformMeleeAttack(_target);
            _fsm.ChangeState(HunterAgent.HunterState.Search);
            return;
        }

        if (distance <= _hunter.RangeAttackRadius)
        {
            _hunter.StopMoving();
            _hunter.PerformRangedAttack(_target);
            _fsm.ChangeState(HunterAgent.HunterState.Search);
            return;
        }

        _hunter.PursueBoid(_target, _hunter.MoveSpeed);
        _hunter.SetFeedback(HunterAgent.HunterState.Attack, _target);
    }

    public override void Exit()
    {
        _hunter.StopMoving();
    }
}
