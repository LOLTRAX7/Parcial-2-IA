using UnityEngine;

[DefaultExecutionOrder(-300)]
public class Bounds : MonoBehaviour
{
    public static Bounds Instance { get; private set; }

    [SerializeField] private float height = 30f;
    [SerializeField] private float width = 60f;
    [SerializeField] private Vector3 center = Vector3.zero;
    [SerializeField] private bool drawGizmos = true;

    public Vector3 Center => center;
    public Vector2 Size => new Vector2(width, height);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() => Instance = null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public Vector3 OutOfBounds(Vector3 position)
    {
        return new Vector3(
            Mathf.Clamp(position.x, center.x - width * 0.5f, center.x + width * 0.5f),
            position.y,
            Mathf.Clamp(position.z, center.z - height * 0.5f, center.z + height * 0.5f));
    }

    public void Constrain(ref Vector3 position, ref Vector3 velocity)
    {
        float minX = center.x - width * 0.5f;
        float maxX = center.x + width * 0.5f;
        float minZ = center.z - height * 0.5f;
        float maxZ = center.z + height * 0.5f;

        if (position.x < minX)
        {
            position.x = minX;
            if (velocity.x < 0f) velocity.x = -velocity.x;
        }
        else if (position.x > maxX)
        {
            position.x = maxX;
            if (velocity.x > 0f) velocity.x = -velocity.x;
        }

        if (position.z < minZ)
        {
            position.z = minZ;
            if (velocity.z < 0f) velocity.z = -velocity.z;
        }
        else if (position.z > maxZ)
        {
            position.z = maxZ;
            if (velocity.z > 0f) velocity.z = -velocity.z;
        }
    }

    public Vector3 GetRandomPointInside()
    {
        float x = Random.Range(center.x - width * 0.5f, center.x + width * 0.5f);
        float z = Random.Range(center.z - height * 0.5f, center.z + height * 0.5f);
        return new Vector3(x, 0f, z);
    }

    public void Configure(float newWidth, float newHeight)
    {
        width = newWidth;
        height = newHeight;
        center = Vector3.zero;
    }

    public void Configure(Vector3 newCenter, float newWidth, float newHeight)
    {
        center = new Vector3(newCenter.x, 0f, newCenter.z);
        width = newWidth;
        height = newHeight;
    }

    public bool Contains(Vector3 position)
    {
        return Mathf.Abs(position.x - center.x) <= width * 0.5f &&
               Mathf.Abs(position.z - center.z) <= height * 0.5f;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(new Vector3(center.x, 0.05f, center.z), new Vector3(width, 0f, height));
    }
}
