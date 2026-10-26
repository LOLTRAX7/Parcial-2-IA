using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(HunterAgent))]
public class HunterFeedback : MonoBehaviour
{
    [SerializeField] private Text _label;
    [SerializeField] private RectTransform _canvas;
    private HunterAgent _hunter;
    private Camera _camera;
    private GraphNavigator _navigator;

    private void Awake()
    {
        _hunter = GetComponent<HunterAgent>();
        _camera = Camera.main;
        _navigator = GetComponent<GraphNavigator>();
    }

    private void LateUpdate()
    {
        if (_label == null || _canvas == null) return;
        if (_camera != null) _canvas.rotation = _camera.transform.rotation;

        string state = _hunter.CurrentState.ToString();
        if (_hunter.IsLegacyMode && _hunter.CurrentState == HunterAgent.HunterState.Search)
            state = "Patrol";

        string gathers = _hunter.Gathers.ToString();
        if (!_hunter.IsLegacyMode) gathers += " / " + _hunter.RequiredGathers;
        string target = _hunter.DebugTarget;
        if (_hunter.CurrentState == HunterAgent.HunterState.Flee && _hunter.ChosenSafeZone != null)
            target = _hunter.ChosenSafeZone.name;
        else if (_hunter.CurrentState == HunterAgent.HunterState.Exit && _hunter.ExitZone != null)
            target = _hunter.ExitZone.name;

        string navigation = _navigator != null && _navigator.UsingPathfinding ? " | A*" : "";
        _label.text = name + ": " + state + navigation + "\nTarget: " + target
            + "\nGathered: " + gathers + " | Wolves: " + _hunter.VisibleWolfCount;
    }
}
