using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GraphManager : MonoBehaviour
{
    public float maxDistance = 2.0f; // jarak maksimum antar node yang masih dianggap terhubung
    private List<PointNode> nodes = new List<PointNode>();

    void Start()
    {
        BuildConnections();
    }

    // ---------------------------------------------------------------
    // MEMBANGUN GRAPH DARI POINT-NODE YANG ADA
    // ---------------------------------------------------------------
    public void BuildConnections()
    {
        nodes = new List<PointNode>(FindObjectsOfType<PointNode>());
        Debug.Log($"[GraphManager] BuildConnections: found {nodes.Count} nodes");

        foreach (var n in nodes)
            n.neighbors.Clear();

        float maxDistSqr = maxDistance * maxDistance;

        // sambungkan node berdekatan (dua arah)
        for (int i = 0; i < nodes.Count; i++)
        {
            var a = nodes[i];
            for (int j = i + 1; j < nodes.Count; j++)
            {
                var b = nodes[j];
                float distSqr = (a.transform.position - b.transform.position).sqrMagnitude;

                if (distSqr <= maxDistSqr)
                {
                    a.neighbors.Add(b);
                    b.neighbors.Add(a);
                }
            }
        }

        Debug.Log("[GraphManager] BuildConnections selesai.");
    }

    // ---------------------------------------------------------------
    // CARI JALUR TERPENDEK MENGGUNAKAN DIJKSTRA (OPTIMIZED)
    // ---------------------------------------------------------------
    public void FindShortestPath(PointNode start, List<PointNode> destinations)
    {
        if (start == null || destinations == null || destinations.Count == 0)
        {
            Debug.LogWarning("[GraphManager] Start atau destinasi belum dipilih!");
            return;
        }

        if (nodes == null || nodes.Count == 0)
        {
            Debug.LogWarning("[GraphManager] Node list kosong — memanggil BuildConnections() ulang.");
            BuildConnections();
        }

        // Jalankan Dijkstra sekali untuk semua tujuan
        var prev = DijkstraAll(start);

        foreach (var dest in destinations)
        {
            var path = ReconstructPath(prev, start, dest);
            if (path.Count > 1)
            {
                DrawPath(path);
                Debug.Log($"[GraphManager] Jalur ditemukan dari {start.name} ke {dest.name} ({path.Count} titik)");
            }
            else
            {
                Debug.LogWarning($"[GraphManager] Tidak ada jalur dari {start.name} ke {dest.name}");
            }
        }
    }

    // ---------------------------------------------------------------
    // DIJKSTRA: SEKALI JALAN UNTUK SEMUA NODE
    // ---------------------------------------------------------------
    private Dictionary<PointNode, PointNode> DijkstraAll(PointNode start)
    {
        var dist = new Dictionary<PointNode, float>(nodes.Count);
        var prev = new Dictionary<PointNode, PointNode>(nodes.Count);
        var visited = new HashSet<PointNode>();

        foreach (var n in nodes)
            dist[n] = float.PositiveInfinity;

        dist[start] = 0f;

        // manual priority queue sederhana
        var queue = new List<PointNode> { start };

        while (queue.Count > 0)
        {
            // ambil node dengan jarak terkecil
            PointNode current = null;
            float minDist = float.PositiveInfinity;
            for (int i = 0; i < queue.Count; i++)
            {
                var node = queue[i];
                if (dist[node] < minDist)
                {
                    minDist = dist[node];
                    current = node;
                }
            }
            queue.Remove(current);
            visited.Add(current);

            // periksa semua tetangga
            foreach (var neighbor in current.neighbors)
            {
                if (visited.Contains(neighbor)) continue;

                float alt = dist[current] + (current.transform.position - neighbor.transform.position).sqrMagnitude;

                if (alt < dist[neighbor])
                {
                    dist[neighbor] = alt;
                    prev[neighbor] = current;

                    if (!queue.Contains(neighbor))
                        queue.Add(neighbor);
                }
            }
        }

        return prev;
    }

    // ---------------------------------------------------------------
    // MEMBANGUN URUTAN NODE DARI HASIL DIJKSTRA
    // ---------------------------------------------------------------
    private List<PointNode> ReconstructPath(Dictionary<PointNode, PointNode> prev, PointNode start, PointNode end)
    {
        var path = new List<PointNode>();

        if (!prev.ContainsKey(end) && end != start)
            return path; // tidak ada jalur

        for (var node = end; node != null; node = prev.ContainsKey(node) ? prev[node] : null)
        {
            path.Insert(0, node);
            if (node == start)
                break;
        }

        return path;
    }

    // ---------------------------------------------------------------
    // GAMBAR GARIS (JALUR)
    // ---------------------------------------------------------------
    private void DrawPath(List<PointNode> path)
    {
        GameObject lineObj = new GameObject($"Path_{path[0].name}_to_{path[^1].name}");
        lineObj.transform.SetParent(transform);

        var lr = lineObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = path.Count;
        lr.material = new Material(Shader.Find("Unlit/Color"));
        lr.material.color = Color.cyan;
        lr.widthMultiplier = 0.05f;

        for (int i = 0; i < path.Count; i++)
            lr.SetPosition(i, path[i].transform.position);
    }

    // ---------------------------------------------------------------
    // RESET SEMUA GARIS
    // ---------------------------------------------------------------
    public void ClearAllPaths()
    {
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Path_"))
                Destroy(child.gameObject);
        }
        Debug.Log("[GraphManager] Semua garis path dihapus.");
    }
}
