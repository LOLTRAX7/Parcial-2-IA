using UnityEngine;

public class HunterDeadState : State<HunterAgent.HunterState>
{
    private readonly HunterAgent _hunter;
    private float _respawnTimer;
    public HunterDeadState(HunterAgent hunter)
    {
        _hunter = hunter;
    }
    public override void Enter()
    {
        _respawnTimer = _hunter.RespawnTime;

        _hunter.StopMoving();
        _hunter.SetBodyVisible(false);
        _hunter.SetFeedback(HunterAgent.HunterState.Dead, null);

        Debug.Log("[Hunter] DEAD. Respawning in the initial zone in " + _respawnTimer.ToString("0.0") + " s.");
    }
    public override void Update()
    {
        _respawnTimer -= Time.deltaTime;

        if (_respawnTimer > 0f) return;

        _hunter.RespawnAtInitialZone();
        Debug.Log("[Hunter] Respawned in the initial zone with the gather count reset.");
        _fsm.ChangeState(HunterAgent.HunterState.Search);
    }
    public override void Exit()
    {
    }
}
