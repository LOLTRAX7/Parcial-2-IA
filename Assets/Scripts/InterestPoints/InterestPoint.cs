using UnityEngine;

public class InterestPoint : MonoBehaviour
{
    [SerializeField] private float maxDurability = 30f;
    [Tooltip("Optional damage color feedback, from green to red.")]
    [SerializeField] private Renderer feedbackRenderer;

    private float _currentDurability;

    private void Awake()
    {
        GameLayers.SetLayer(gameObject, GameLayers.Interest);
        if (GetComponent<Collider>() == null)
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.5f;
        }

        if (feedbackRenderer == null) feedbackRenderer = GetComponentInChildren<Renderer>();
    }

    public void Initialize(Renderer renderer)
    {
        feedbackRenderer = renderer;
        UpdateFeedback();
    }

    private void OnEnable()
    {
        _currentDurability = maxDurability;
        UpdateFeedback();
    }

    public void TakeDamage(float amount)
    {
        _currentDurability -= amount;
        UpdateFeedback();

        if (_currentDurability <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void UpdateFeedback()
    {
        if (feedbackRenderer == null) return;
        float t = Mathf.Clamp01(_currentDurability / maxDurability);
        feedbackRenderer.material.color = Color.Lerp(Color.red, Color.green, t);
    }
}
