using UnityEngine;

public static class FeedbackMaterials
{
    public static Material CreateUnlit(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.color = color;
        return material;
    }
}
