using System.Collections.Generic;
using UnityEngine;

public class WolfSearchState : State<WolfAgent.WolfState>
{
    private readonly WolfAgent _wolf;
    private readonly List<Node> _candidates = new List<Node>();

    private float _timer;
    private float _scanTimer;
    private bool _scanning;
    private int _index;

    public WolfSearchState(WolfAgent wolf)
    {
        _wolf = wolf;
    }

    public override void Enter()
    {
        _timer = _wolf.SearchDurationForState;
        _scanTimer = 0f;
        _scanning = false;
        _index = 0;

        ChooseSearchPoints();
        FeedbackLog.Add(_wolf.name + " searches for the hunter near " + WolfAgent.Format(_wolf.LastKnownPosition) + ".", new Color(1f, 0.9f, 0.2f), false);
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
            _fsm.ChangeState(WolfAgent.WolfState.GoToPosition);
            return;
        }

        _timer -= Time.deltaTime;

        if (_timer <= 0f)
        {
            _timer = _wolf.SearchDurationForState;
            ChooseSearchPoints();
            _index = _candidates.Count > 0 ? _index % _candidates.Count : 0;
        }

        if (_candidates.Count == 0)
        {
            _wolf.Scan(120f);
            return;
        }

        if (_scanning)
        {
            _wolf.Scan(150f);
            _scanTimer -= Time.deltaTime;

            if (_scanTimer <= 0f)
            {
                _scanning = false;
                _index = (_index + 1) % _candidates.Count;
            }

            return;
        }

        Vector3 point = _candidates[_index].transform.position;
        _wolf.MoveTo(point, _wolf.AlertSpeed);

        if (!_wolf.HasReached(point, 1f)) return;

        _scanning = true;
        _scanTimer = _wolf.ScanDuration;
    }

    private void ChooseSearchPoints()
    {
        _candidates.Clear();

        Graph graph = _wolf.Graph;

        if (graph == null) return;

        graph.GetNodesWithin(_wolf.LastKnownPosition, _wolf.SearchRadius, _candidates, true);

        if (_candidates.Count == 0)
        {
            Node closest = graph.GetClosestNode(_wolf.LastKnownPosition, true);
            if (closest != null) _candidates.Add(closest);
        }
    }

    public override void Exit() => _wolf.StopMoving();
}
