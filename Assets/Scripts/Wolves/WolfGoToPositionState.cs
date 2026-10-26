using UnityEngine;

public class WolfGoToPositionState : State<WolfAgent.WolfState>
{
    private readonly WolfAgent _wolf;
    private Vector3 _destination;

    public WolfGoToPositionState(WolfAgent wolf)
    {
        _wolf = wolf;
    }

    public override void Enter()
    {
        _wolf.ConsumePendingAlert();

        if (!_wolf.HasKnownPosition || Zone.IsInsideSafeZone(_wolf.LastKnownPosition))
        {
            _fsm.ChangeState(WolfAgent.WolfState.Patrol);
            return;
        }

        _destination = _wolf.LastKnownPosition;
        FeedbackLog.Add(_wolf.name + " moves to the last known position " + WolfAgent.Format(_destination) + ".", new Color(1f, 0.65f, 0.2f), false);
    }

    public override void Update()
    {
        if (_wolf.HunterInsideSafeZone || _wolf.HunterIsDead)
        {
            _wolf.ForgetHunter();
            _fsm.ChangeState(WolfAgent.WolfState.Patrol);
            return;
        }

        if (_wolf.CanSeeHunter)
        {
            _fsm.ChangeState(WolfAgent.WolfState.Chase);
            return;
        }

        if (_wolf.HasPendingAlert)
        {
            _wolf.ConsumePendingAlert();
            _destination = _wolf.LastKnownPosition;

            FeedbackLog.Add(_wolf.name + " updates its known position to " + WolfAgent.Format(_destination) + ".", new Color(1f, 0.65f, 0.2f), false);
        }

        if (!_wolf.HasKnownPosition || Zone.IsInsideSafeZone(_destination))
        {
            _wolf.ForgetHunter();
            _fsm.ChangeState(WolfAgent.WolfState.Patrol);
            return;
        }

        _wolf.MoveTo(_destination, _wolf.AlertSpeed);

        if (!_wolf.HasReached(_destination, 1.2f)) return;

        _fsm.ChangeState(WolfAgent.WolfState.Search);
    }

    public override void Exit() => _wolf.StopMoving();
}
