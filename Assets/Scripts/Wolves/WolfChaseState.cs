using UnityEngine;

public class WolfChaseState : State<WolfAgent.WolfState>
{
    private readonly WolfAgent _wolf;

    public WolfChaseState(WolfAgent wolf)
    {
        _wolf = wolf;
    }

    public override void Enter()
    {
        if (_wolf.HunterIsDead || _wolf.HunterInsideSafeZone)
        {
            _fsm.ChangeState(WolfAgent.WolfState.Patrol);
            return;
        }

        if (!_wolf.CanSeeHunter)
        {
            _fsm.ChangeState(WolfAgent.WolfState.GoToPosition);
            return;
        }

        _wolf.NotifyTargetSpotted();
    }

    public override void Update()
    {
        if (_wolf.HunterIsDead)
        {
            _wolf.ForgetHunter();
            _fsm.ChangeState(WolfAgent.WolfState.Patrol);
            return;
        }

        if (_wolf.HunterInsideSafeZone)
        {
            FeedbackLog.Add(_wolf.name + " stops chasing: the hunter entered a safe zone.", new Color(0.65f, 0.5f, 0.92f));
            _wolf.ForgetHunter();
            _fsm.ChangeState(WolfAgent.WolfState.Patrol);
            return;
        }

        if (!_wolf.CanSeeHunter)
        {
            _fsm.ChangeState(WolfAgent.WolfState.GoToPosition);
            return;
        }

        HunterAgent hunter = _wolf.Hunter;
        _wolf.NotifyTargetSpotted();
        _wolf.MoveTo(hunter.transform.position, _wolf.ChaseSpeed);

        if (!_wolf.TryCaptureHunter()) return;

        _wolf.ForgetHunter();
        _fsm.ChangeState(WolfAgent.WolfState.Patrol);
    }

    public override void Exit() => _wolf.StopMoving();
}
