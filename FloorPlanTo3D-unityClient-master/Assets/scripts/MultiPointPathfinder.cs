using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class MultiPointPathfinder : MonoBehaviour
{
    public NavMeshSurface navSurface;
    public LineRenderer lineRenderer;
    public ClickPointSelector selector;

    void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
    }

    public void CalculateMultiPath()
    {
        if (selector.GetStartMarker() == null || selector.goalMarkers.Count == 0)
        {
            Debug.LogWarning("⚠️ Start dan minimal 1 Goal harus ditentukan dulu!");
            return;
        }

        Vector3 currentPos = selector.GetStartMarker().position;
        List<Vector3> fullPathPoints = new List<Vector3> { currentPos };
        List<Transform> remaining = new List<Transform>(selector.goalMarkers);

        // 🔁 Greedy approach: pilih goal terdekat berikutnya
        while (remaining.Count > 0)
        {
            Transform nearest = null;
            float nearestDist = float.MaxValue;
            NavMeshPath tempPath = new NavMeshPath();

            foreach (var g in remaining)
            {
                if (NavMesh.CalculatePath(currentPos, g.position, NavMesh.AllAreas, tempPath))
                {
                    float dist = CalculatePathDistance(tempPath);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = g;
                    }
                }
            }

            if (nearest != null)
            {
                NavMeshPath bestPath = new NavMeshPath();
                NavMesh.CalculatePath(currentPos, nearest.position, NavMesh.AllAreas, bestPath);
                fullPathPoints.AddRange(bestPath.corners);
                currentPos = nearest.position;
                remaining.Remove(nearest);
            }
            else break;
        }

        // ✏️ Visualisasi hasil jalur
        lineRenderer.positionCount = fullPathPoints.Count;
        lineRenderer.SetPositions(fullPathPoints.ToArray());
        Debug.Log($"📏 Total path length: {CalculateTotalDistance(fullPathPoints):F2} units");
    }

    float CalculatePathDistance(NavMeshPath path)
    {
        float total = 0f;
        for (int i = 0; i < path.corners.Length - 1; i++)
            total += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        return total;
    }

    float CalculateTotalDistance(List<Vector3> points)
    {
        float total = 0f;
        for (int i = 0; i < points.Count - 1; i++)
            total += Vector3.Distance(points[i], points[i + 1]);
        return total;
    }
}
