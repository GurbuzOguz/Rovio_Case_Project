using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple path definition using local-space waypoints.
/// Box follows the path by adding these local points to path world origin.
/// </summary>
public class BoxPath : MonoBehaviour
{
    [SerializeField] private List<Vector3> localWaypoints = new List<Vector3>();

    public IReadOnlyList<Vector3> LocalWaypoints => localWaypoints;

    private void OnDrawGizmosSelected()
    {
        if (localWaypoints == null || localWaypoints.Count == 0)
            return;

        Gizmos.color = Color.yellow;
        var origin = transform.position;

        Vector3 prev = origin + localWaypoints[0];
        Gizmos.DrawSphere(prev, 0.1f);

        for (int i = 1; i < localWaypoints.Count; i++)
        {
            Vector3 next = origin + localWaypoints[i];
            Gizmos.DrawLine(prev, next);
            Gizmos.DrawSphere(next, 0.1f);
            prev = next;
        }
    }
}

