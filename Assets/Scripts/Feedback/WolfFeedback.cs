using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(WolfAgent))]
public class WolfFeedback : MonoBehaviour
{
    [SerializeField] private Color _patrolColor = new Color(0.75f, 0.75f, 0.78f);
    [SerializeField] private Color _goToPositionColor = new Color(1f, 0.55f, 0.1f);
    [SerializeField] private Color _chaseColor = new Color(1f, 0.15f, 0.15f);
    [SerializeField] private Color _searchColor = new Color(1f, 0.9f, 0.2f);
    [SerializeField] private Color _alertFlashColor = new Color(0.2f, 0.9f, 1f);

    private WolfAgent _wolf;
    private ColorFeedback _colorFeedback;
    private AudioSource _audio;
    [SerializeField] private AudioClip _howlClip;
    [SerializeField] private Text _label;
    [SerializeField] private RectTransform _canvas;
    private Camera _camera;
    private string _message;
    private float _messageUntil;

    private void Awake()
    {
        _wolf = GetComponent<WolfAgent>();
        _colorFeedback = GetComponent<ColorFeedback>();
        _audio = gameObject.GetOrAddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _audio.spatialBlend = 0f;
        _audio.volume = 0.5f;
        _camera = Camera.main;
    }

    private void OnEnable()
    {
        _wolf.OnStateChanged += HandleStateChanged;
        _wolf.OnHowled += HandleHowled;
        _wolf.OnAlertReceived += HandleAlertReceived;
    }

    private void OnDisable()
    {
        _wolf.OnStateChanged -= HandleStateChanged;
        _wolf.OnHowled -= HandleHowled;
        _wolf.OnAlertReceived -= HandleAlertReceived;
    }

    private void HandleStateChanged(WolfAgent.WolfState state)
    {
        Color color = _patrolColor;
        switch (state)
        {
            case WolfAgent.WolfState.Chase: color = _chaseColor; break;
            case WolfAgent.WolfState.GoToPosition: color = _goToPositionColor; break;
            case WolfAgent.WolfState.Search: color = _searchColor; break;
        }
        _colorFeedback.SetColor(color);
    }

    private void HandleHowled(Vector3 position)
    {
        if (_howlClip != null) _audio.PlayOneShot(_howlClip);
        _message = "Hunter spotted | Alert sent";
        _messageUntil = Time.time + 1.5f;
    }

    private void HandleAlertReceived(WolfAgent source, Vector3 position)
    {
        _colorFeedback.Flash(_alertFlashColor, 0.7f);
        _message = "Alert from " + source.name;
        _messageUntil = Time.time + 1.5f;
    }

    private void LateUpdate()
    {
        if (_label == null || _canvas == null) return;
        if (_camera != null) _canvas.rotation = _camera.transform.rotation;
        string known = _wolf.HasKnownPosition ? WolfAgent.Format(_wolf.LastKnownPosition) : "-";
        string navigation = _wolf.Navigator.UsingPathfinding ? " | A*" : "";
        _label.text = name + ": " + _wolf.CurrentState + navigation + "\nKnown: " + known;
        if (Time.time < _messageUntil) _label.text += "\n" + _message;
    }
}
