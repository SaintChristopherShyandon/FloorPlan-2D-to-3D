using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(LineRenderer))]
public class PathFinderNavMesh : MonoBehaviour
{
    public NavMeshSurface navSurface;
    public LineRenderer lineRenderer;
    public Transform startMarker;
    public Transform goalMarker;

    void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
    }

    // Dipanggil setelah floorplan terbentuk
    public void BakeNavMesh()
    {
        if (navSurface != null)
        {
            navSurface.BuildNavMesh();
            Debug.Log("✅ NavMesh built successfully.");
        }
        else
        {
            Debug.LogWarning("⚠️ NavMeshSurface belum di-assign!");
        }
    }

    public void CalculateAndShowPath(Vector3 start, Vector3 goal)
    {
        NavMeshPath path = new NavMeshPath();
        bool success = NavMesh.CalculatePath(start, goal, NavMesh.AllAreas, path);

        if (!success || path.corners.Length == 0)
        {
            Debug.LogWarning("❌ Path tidak ditemukan!");
            lineRenderer.positionCount = 0;
            return;
        }

        lineRenderer.positionCount = path.corners.Length;
        lineRenderer.SetPositions(path.corners);

        float totalDist = 0f;
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            totalDist += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }

        Debug.Log($"📏 Total path distance: {totalDist:F2} units");
    }

    public void FindUsingMarkers()
    {
        if (startMarker == null || goalMarker == null)
        {
            Debug.LogWarning("⚠️ Start atau Goal marker belum diatur!");
            return;
        }
        CalculateAndShowPath(startMarker.position, goalMarker.position);
    }
}
