using UnityEngine;

public class HunterSearchState : State<HunterAgent.HunterState>
{
    private readonly HunterAgent _hunter;

    private Vector3 _wanderTarget;
    private bool _hasWanderTarget;
    private float _interestTimer;

    public HunterSearchState(HunterAgent hunter)
    {
        _hunter = hunter;
    }

    public override void Enter()
    {
        _hasWanderTarget = false;

        _hunter.SetFeedback(HunterAgent.HunterState.Search, null);
        FeedbackLog.Add("[Hunter] Search: moving to the hunting ground.", Color.white, false);
    }

    public override void Update()
    {
        if (_hunter.ShouldFlee)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Flee);
            return;
        }

        if (_hunter.HuntingZone != null && _hunter.HuntingZone.Contains(_hunter.transform.position))
        {
            _interestTimer += Time.deltaTime;
            if (_interestTimer >= _hunter.InterestPointInterval)
            {
                _interestTimer = 0f;
                _hunter.InterestPoints.SpawnInHuntingZone();
            }
        }

        if (_hunter.Gathers >= _hunter.RequiredGathers)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Exit);
            return;
        }

        if (_hunter.Perception.FindNearestEliminatedBoid() != null)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Gather);
            return;
        }

        if (_hunter.IsAttackReady && _hunter.Perception.FindNearestLiveBoid() != null)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Attack);
            return;
        }

        if (_hunter.HuntingZone == null)
        {
            _hunter.StopMoving();
            return;
        }

        if (!_hunter.HuntingZone.Contains(_hunter.transform.position))
        {
            _hunter.MoveTo(_hunter.HuntingZone.Center, _hunter.MoveSpeed);
            _hunter.SetFeedback(HunterAgent.HunterState.Search, null);
            return;
        }

        bool seesLiveBoid = _hunter.Perception.FindNearestLiveBoid() != null;

        if (seesLiveBoid)
        {
            _hunter.StopMoving();
            _hunter.SetFeedback(HunterAgent.HunterState.Search, null);
            return;
        }

        if (!_hasWanderTarget)
        {
            Node node = _hunter.GetRandomHuntingNode();

            if (node == null)
            {
                _hunter.StopMoving();
                return;
            }

            _wanderTarget = node.transform.position;
            _hasWanderTarget = true;
        }

        _hunter.MoveTo(_wanderTarget, _hunter.MoveSpeed);
        _hunter.SetFeedback(HunterAgent.HunterState.Search, null);

        if (_hunter.HasReached(_wanderTarget, 0.8f))
        {
            _hasWanderTarget = false;
        }
    }

    public override void Exit()
    {
        _hunter.StopMoving();
    }
}
