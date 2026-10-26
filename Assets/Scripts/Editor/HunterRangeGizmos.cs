using UnityEditor;
using UnityEngine;

public static class HunterRangeGizmos
{
    [DrawGizmo(GizmoType.Selected)]
    private static void DrawHunter(HunterAgent hunter, GizmoType gizmoType)
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(hunter.transform.position, hunter.VisionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(hunter.transform.position, hunter.MeleeAttackRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(hunter.transform.position, hunter.RangeAttackRadius);
    }
}
