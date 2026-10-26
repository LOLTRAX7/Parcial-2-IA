public abstract class State<T>
{
    protected FiniteStateMachine<T> _fsm;

    public void SetFSM(FiniteStateMachine<T> fsm) => _fsm = fsm;

    public abstract void Enter();
    public abstract void Update();
    public abstract void Exit();
}
