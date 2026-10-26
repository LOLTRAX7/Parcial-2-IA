using UnityEngine;

public static class GameObjectExtensions
{
    public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
    {
        if (!gameObject.TryGetComponent(out T component)) component = gameObject.AddComponent<T>();

        return component;
    }
}
