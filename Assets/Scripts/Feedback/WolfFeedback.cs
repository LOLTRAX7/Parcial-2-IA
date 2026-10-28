using UnityEngine;

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
    private string _text;
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
        _message = "Alerta enviada";
        _messageUntil = Time.time + 2f;
    }

    private void HandleAlertReceived(WolfAgent source, Vector3 position)
    {
        _colorFeedback.Flash(_alertFlashColor, 0.7f);
        _message = "Alerta recibida de " + FeedbackLabel.AgentName(source.name);
        _messageUntil = Time.time + 2f;
    }

    private void LateUpdate()
    {
        _text = FeedbackLabel.AgentName(name) + ": " + StateName();
        if (_wolf.Navigator.UsingPathfinding) _text += " | Ruta A*";
        if (_wolf.CanSeeHunter) _text += "\nCazador detectado";
        if (_wolf.HasKnownPosition) _text += "\nÚltima posición: " + WolfAgent.Format(_wolf.LastKnownPosition);
        if (Time.time < _messageUntil) _text += "\n" + _message;
    }

    private string StateName()
    {
        switch (_wolf.CurrentState)
        {
            case WolfAgent.WolfState.Patrol: return "Patrullando";
            case WolfAgent.WolfState.Chase: return "Persiguiendo";
            case WolfAgent.WolfState.GoToPosition: return "Hacia última posición";
            case WolfAgent.WolfState.Search: return "Buscando";
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
        if (!Application.isPlaying || _wolf == null || !_wolf.HasKnownPosition) return;
        Gizmos.color = _goToPositionColor;
        Gizmos.DrawWireSphere(_wolf.LastKnownPosition + Vector3.up * 0.2f, 0.5f);
    }
}
