using UnityEngine;

public static class FeedbackLabel
{
    public static string AgentName(string value)
    {
        if (string.IsNullOrEmpty(value) || value == "-") return "ninguno";
        return value.Replace("WolfWest", "Lobo oeste").Replace("WolfEast", "Lobo este")
            .Replace("WolfNorth", "Lobo norte").Replace("WolfSouth", "Lobo sur")
            .Replace("Boid_", "Oveja ").Replace("SafeZoneA", "Refugio A")
            .Replace("SafeZoneB", "Refugio B").Replace("Hunter", "Cazador");
    }

    public static void DrawGame(Vector3 position, string text)
    {
        Camera camera = Camera.main;
        if (camera == null || string.IsNullOrEmpty(text)) return;
        Vector3 point = camera.WorldToScreenPoint(position);
        if (point.z <= 0f || point.x < 0f || point.x > Screen.width || point.y < 0f || point.y > Screen.height) return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.MiddleLeft;
        style.fontSize = 12;
        style.normal.textColor = Color.white;
        style.normal.background = Texture2D.blackTexture;
        Vector2 size = style.CalcSize(new GUIContent(text));
        size += new Vector2(12f, 8f);
        float x = Mathf.Clamp(point.x - size.x * 0.5f, 4f, Mathf.Max(4f, Screen.width - size.x - 4f));
        float y = Mathf.Clamp(Screen.height - point.y - size.y, 4f, Mathf.Max(4f, Screen.height - size.y - 4f));
        GUI.Box(new Rect(x, y, size.x, size.y), text, style);
    }

    public static void DrawScene(Vector3 position, string text)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying || string.IsNullOrEmpty(text)) return;
        GUIStyle style = new GUIStyle(UnityEditor.EditorStyles.helpBox);
        style.normal.textColor = Color.white;
        style.normal.background = Texture2D.blackTexture;
        UnityEditor.Handles.Label(position, text, style);
#endif
    }
}
