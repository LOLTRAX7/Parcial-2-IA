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
        if (_wolf.Hunter == null || _wolf.Hunter.IsDead || _wolf.Hunter.IsInSafeZone)
        {
            _fsm.ChangeState(WolfAgent.WolfState.Patrol);
            return;
        }

        _wolf.NotifyTargetSpotted();
    }

    public override void Update()
    {
        HunterAgent hunter = _wolf.Hunter;

        if (hunter == null || hunter.IsDead)
        {
            _wolf.ForgetHunter();
            _fsm.ChangeState(WolfAgent.WolfState.Patrol);
            return;
        }

        if (hunter.IsInSafeZone)
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

        _wolf.NotifyTargetSpotted();
        _wolf.MoveTo(hunter.transform.position, _wolf.ChaseSpeed);

        if (!_wolf.TryCaptureHunter()) return;

        _wolf.ForgetHunter();
        _fsm.ChangeState(WolfAgent.WolfState.Patrol);
    }

    public override void Exit() => _wolf.StopMoving();
}
