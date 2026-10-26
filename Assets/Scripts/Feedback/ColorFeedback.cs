using UnityEngine;

public class ColorFeedback : MonoBehaviour
{
    [SerializeField] private Renderer _renderer;

    private Material _material;
    private Color _baseColor = Color.white;
    private Color _stateColor = Color.white;
    private Color _flashColor;
    private float _flashUntil;

    public Color BaseColor => _baseColor;

    public void SetRenderer(Renderer renderer)
    {
        _renderer = renderer;
        _material = null;

        if (_renderer == null) return;

        _material = _renderer.material;
        _baseColor = _material.color;
        _stateColor = _baseColor;
    }

    public void SetColor(Color color)
    {
        _stateColor = color;
        Apply();
    }

    public void Flash(Color color, float duration)
    {
        _flashColor = color;
        _flashUntil = Time.time + duration;
        Apply();
    }

    private void Update()
    {
        Apply();
    }

    private void Apply()
    {
        if (_material == null) return;

        _material.color = Time.time < _flashUntil ? _flashColor : _stateColor;
    }

    private void OnValidate()
    {
        if (_renderer == null) TryGetComponent(out _renderer);
    }
}
