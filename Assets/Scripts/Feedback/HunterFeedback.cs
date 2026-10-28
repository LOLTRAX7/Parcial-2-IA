using UnityEngine;

[RequireComponent(typeof(HunterAgent))]
public class HunterFeedback : MonoBehaviour
{
    private HunterAgent _hunter;
    private GraphNavigator _navigator;
    private string _text;

    private void Awake()
    {
        _hunter = GetComponent<HunterAgent>();
        _navigator = GetComponent<GraphNavigator>();
    }

    private void LateUpdate()
    {
        string state = StateName();
        if (_hunter.IsLegacyMode && _hunter.CurrentState == HunterAgent.HunterState.Search) state = "Patrullando";
        _text = "Cazador: " + state + " | Recolectados: " + _hunter.Gathers;
        if (!_hunter.IsLegacyMode) _text += "/" + _hunter.RequiredGathers;
        if (_navigator != null && _navigator.UsingPathfinding) _text += " | Ruta A*";
        if (_hunter.VisibleWolfCount > 0) _text += "\nLobo detectado";
        if (_hunter.CurrentState == HunterAgent.HunterState.Flee && _hunter.ChosenSafeZone != null)
            _text += "\nRefugio: " + FeedbackLabel.AgentName(_hunter.ChosenSafeZone.name);
        else if (_hunter.CurrentState == HunterAgent.HunterState.Attack || _hunter.CurrentState == HunterAgent.HunterState.Gather)
            _text += "\nObjetivo: " + FeedbackLabel.AgentName(_hunter.DebugTarget);
    }

    private string StateName()
    {
        switch (_hunter.CurrentState)
        {
            case HunterAgent.HunterState.Search: return "Buscando";
            case HunterAgent.HunterState.Attack: return "Atacando";
            case HunterAgent.HunterState.Gather: return "Recolectando";
            case HunterAgent.HunterState.Flee: return "Huyendo";
            case HunterAgent.HunterState.Exit: return "Hacia la salida";
            case HunterAgent.HunterState.Dead: return "Eliminado";
            default: return "";
        }
    }

    private void OnGUI()
    {
        FeedbackLabel.DrawGame(transform.position + Vector3.up * 2f, _text);
    }

    private void OnDrawGizmos()
    {
        FeedbackLabel.DrawScene(transform.position + Vector3.up * 2f, _text);
    }
}
