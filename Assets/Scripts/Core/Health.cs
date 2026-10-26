using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    public event Action<float> OnDamaged = delegate { };
    public event Action OnDied = delegate { };

    [SerializeField] private float _maxHealth = 100f;

    public float Max => _maxHealth;
    public float Current { get; private set; }
    public bool IsDead { get; private set; }

    public float Normalized => _maxHealth > 0f ? Current / _maxHealth : 0f;

    private void Awake()
    {
        Current = _maxHealth;
    }

    public void Initialize(float maxHealth)
    {
        _maxHealth = maxHealth;
        ResetHealth();
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        Current = Mathf.Max(0f, Current - amount);
        OnDamaged?.Invoke(amount);

        if (Current > 0f) return;

        IsDead = true;
        OnDied?.Invoke();
    }

    public void ResetHealth()
    {
        Current = _maxHealth;
        IsDead = false;
    }
}
