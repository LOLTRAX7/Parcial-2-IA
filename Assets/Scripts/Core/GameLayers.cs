using UnityEngine;

public static class GameLayers
{
    public static int Wall => Resolve("Wall", 8);
    public static int Hunter => Resolve("Hunter", 9);
    public static int Wolf => Resolve("Wolf", 10);
    public static int Boid => Resolve("Boid", 11);
    public static int Interest => Resolve("Interest", 12);

    public static LayerMask WallMask => 1 << Wall;
    public static LayerMask HunterMask => 1 << Hunter;
    public static LayerMask WolfMask => 1 << Wolf;
    public static LayerMask BoidMask => 1 << Boid;
    public static LayerMask InterestMask => 1 << Interest;

    private static int Resolve(string layerName, int fallback)
    {
        int layer = LayerMask.NameToLayer(layerName);
        return layer >= 0 ? layer : fallback;
    }

    public static void SetLayer(GameObject target, int layer)
    {
        target.layer = layer;

        foreach (Transform child in target.transform)
        {
            SetLayer(child.gameObject, layer);
        }
    }
}
