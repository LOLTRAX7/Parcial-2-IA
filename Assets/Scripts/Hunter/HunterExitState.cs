using UnityEngine;

public class HunterExitState : State<HunterAgent.HunterState>
{
    private readonly HunterAgent _hunter;
    private bool _completing;
    private float _completeTimer;

    public HunterExitState(HunterAgent hunter)
    {
        _hunter = hunter;
    }

    public override void Enter()
    {
        _completing = false;
        _completeTimer = 0f;

        _hunter.SetFeedback(HunterAgent.HunterState.Exit, null);
        FeedbackLog.Add("[Hunter] Exit: gathered " + _hunter.Gathers + "/" + _hunter.RequiredGathers + ". Moving to the exit.", new Color(0.3f, 1f, 0.4f));
    }

    public override void Update()
    {
        if (_hunter.ShouldFlee)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Flee);
            return;
        }

        if (_completing)
        {
            _completeTimer -= Time.deltaTime;

            if (_completeTimer > 0f) return;

            _hunter.RespawnAtInitialZone();
            FeedbackLog.Add("[Hunter] Objective complete: respawning in the initial zone for a new cycle.", new Color(0.3f, 1f, 0.4f));
            _fsm.ChangeState(HunterAgent.HunterState.Search);
            return;
        }

        if (_hunter.Gathers < _hunter.RequiredGathers)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Search);
            return;
        }

        _hunter.MoveTo(_hunter.ExitZone.Center, _hunter.MoveSpeed);
        _hunter.SetFeedback(HunterAgent.HunterState.Exit, null);

        if (!_hunter.HasReached(_hunter.ExitZone.Center, 1.2f)) return;

        _completing = true;
        _completeTimer = 0.6f;
        _hunter.StopMoving();
        _hunter.SetBodyVisible(false);
        _hunter.SetFeedback(HunterAgent.HunterState.Exit, null);
        FeedbackLog.Add("[Hunter] Reached the exit. Disappearing.", new Color(0.3f, 1f, 0.4f), false);
    }

    public override void Exit()
    {
        _hunter.StopMoving();
    }
}
