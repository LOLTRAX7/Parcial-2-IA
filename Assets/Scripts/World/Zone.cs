using System.Collections.Generic;
using UnityEngine;

public enum ZoneType
{
    Initial,
    Hunting,
    Safe,
    Exit,
    Patrol,
}

[ExecuteAlways]
public class Zone : MonoBehaviour
{
    [SerializeField] private ZoneType _type = ZoneType.Patrol;
    [SerializeField] private Vector2 _size = new Vector2(2f, 2f);

    private static readonly List<Zone> _all = new List<Zone>();
    private static readonly List<Zone> _safeZones = new List<Zone>();

    public static IReadOnlyList<Zone> All => _all;
    public static IReadOnlyList<Zone> SafeZones => _safeZones;

    public ZoneType Type => _type;
    public Vector2 Size => _size;
    public Vector3 Center => transform.position;

    public UnityEngine.Bounds WorldBounds => new UnityEngine.Bounds(new Vector3(Center.x, 0.5f, Center.z), new Vector3(_size.x, 1f, _size.y));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _all.Clear();
        _safeZones.Clear();
    }

    private void OnEnable()
    {
        if (!_all.Contains(this)) _all.Add(this);
        if (_type == ZoneType.Safe && !_safeZones.Contains(this)) _safeZones.Add(this);

    }

    private void OnDisable()
    {
        _all.Remove(this);
        _safeZones.Remove(this);

    }

    public void Configure(ZoneType type, Vector3 center, Vector2 size)
    {
        _type = type;
        transform.position = new Vector3(center.x, 0f, center.z);
        _size = size;

        _all.Remove(this);
        _safeZones.Remove(this);

        _all.Add(this);
        if (_type == ZoneType.Safe) _safeZones.Add(this);

    }

    public bool Contains(Vector3 worldPosition, float inset = 0f)
    {
        Vector3 offset = worldPosition - Center;
        return Mathf.Abs(offset.x) + inset <= _size.x * 0.5f
            && Mathf.Abs(offset.z) + inset <= _size.y * 0.5f;
    }

    public Vector3 ClosestPoint(Vector3 worldPosition)
    {
        float halfX = _size.x * 0.5f;
        float halfZ = _size.y * 0.5f;

        return new Vector3(
            Mathf.Clamp(worldPosition.x, Center.x - halfX, Center.x + halfX),
            0f,
            Mathf.Clamp(worldPosition.z, Center.z - halfZ, Center.z + halfZ));
    }

    public Vector3 RandomPoint()
    {
        float halfX = _size.x * 0.5f;
        float halfZ = _size.y * 0.5f;

        return new Vector3(
            UnityEngine.Random.Range(Center.x - halfX, Center.x + halfX),
            0f,
            UnityEngine.Random.Range(Center.z - halfZ, Center.z + halfZ));
    }

    public static bool IsInsideSafeZone(Vector3 worldPosition, float inset = 0f)
    {
        for (int i = 0; i < _safeZones.Count; i++)
        {
            if (_safeZones[i] != null && _safeZones[i].Contains(worldPosition, inset)) return true;
        }

        return false;
    }

    public static bool CrossesSafeZone(Vector3 from, Vector3 to, float radius = 0f)
    {
        Vector3 start = new Vector3(from.x, 0.5f, from.z);
        Vector3 end = new Vector3(to.x, 0.5f, to.z);
        Vector3 offset = end - start;
        float distance = offset.magnitude;
        foreach (Zone zone in _safeZones)
        {
            if (zone == null) continue;
            UnityEngine.Bounds area = zone.WorldBounds;
            area.Expand(new Vector3(radius * 2f, 0f, radius * 2f));
            if (area.Contains(start) || area.Contains(end)) return true;
            if (distance > 0.001f && area.IntersectRay(new Ray(start, offset / distance), out float hit)
                && hit <= distance) return true;
        }
        return false;
    }

    public static Color ColorFor(ZoneType type)
    {
        return type switch
        {
            ZoneType.Initial => new Color(0.35f, 0.85f, 0.45f),
            ZoneType.Hunting => new Color(0.95f, 0.82f, 0.32f),
            ZoneType.Safe => new Color(0.65f, 0.5f, 0.92f),
            ZoneType.Exit => new Color(0.3f, 0.78f, 0.92f),
            _ => new Color(0.65f, 0.65f, 0.65f),
        };
    }

    private void OnDrawGizmosSelected()
    {
        Color color = ColorFor(_type);
        Vector3 size = new Vector3(_size.x, 0.2f, _size.y);

        Gizmos.color = new Color(color.r, color.g, color.b, 0.15f);
        Gizmos.DrawCube(new Vector3(Center.x, 0.1f, Center.z), size);

        Gizmos.color = color;
        Gizmos.DrawWireCube(new Vector3(Center.x, 0.1f, Center.z), size);
    }
}
