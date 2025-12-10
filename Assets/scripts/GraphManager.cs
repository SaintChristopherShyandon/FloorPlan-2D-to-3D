using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics; 
using Debug = UnityEngine.Debug;

public class GraphManager : MonoBehaviour
{
    [Header("Graph")]
    public float maxDistance = 2.0f;
    public bool use2D = false;
    public bool logTimings = true;

    private List<PointNode> nodes = new List<PointNode>();
    private static Material sharedLineMaterial;

    // >>> DITAMBAHKAN
    [Header("Distance Result")]
    public float lastTotalDistance = 0f;              // total meter
    public List<PointNode> lastPath = new List<PointNode>(); // path final

    void Start()
    {
        BuildConnections();
    }

    // ===========================================================
    // BUILD CONNECTIONS
    // ===========================================================
    public void BuildConnections()
    {
        nodes = new List<PointNode>(FindObjectsOfType<PointNode>());
        Debug.Log($"[GraphManager] BuildConnections: found {nodes.Count} nodes");

        foreach (var n in nodes)
            n.neighbors.Clear();

        if (nodes.Count == 0) return;

        Stopwatch sw = null;
        if (logTimings) sw = Stopwatch.StartNew();

        BuildConnectionsSpatialHash(nodes, maxDistance, use2D);

        if (logTimings && sw != null)
        {
            sw.Stop();
            Debug.Log($"[GraphManager] BuildConnections selesai. Time = {sw.Elapsed.TotalMilliseconds:F2} ms");
        }
    }

    private static void BuildConnectionsSpatialHash(List<PointNode> nodes, float cellSize, bool twoD)
    {
        int n = nodes.Count;
        float maxDistSqr = cellSize * cellSize;

        var positions = new Vector3[n];
        var indexOf = new Dictionary<PointNode, int>(n);

        for (int i = 0; i < n; i++)
        {
            positions[i] = nodes[i].transform.position;
            indexOf[nodes[i]] = i;
        }

        var grid = new Dictionary<Vector3Int, List<int>>(n);

        for (int i = 0; i < n; i++)
        {
            var cell = Hash(positions[i], cellSize, twoD);
            if (!grid.TryGetValue(cell, out var list))
            {
                list = new List<int>(8);
                grid[cell] = list;
            }
            list.Add(i);
        }

        var offsets = BuildNeighborOffsets(twoD);

        for (int i = 0; i < n; i++)
        {
            var cell = Hash(positions[i], cellSize, twoD);
            var posA = positions[i];

            foreach (var off in offsets)
            {
                var neighborCell = cell + off;
                if (!grid.TryGetValue(neighborCell, out var list)) continue;

                for (int k = 0; k < list.Count; k++)
                {
                    int j = list[k];
                    if (j <= i) continue;

                    var posB = positions[j];
                    float distSqr = (posA - posB).sqrMagnitude;

                    if (distSqr <= maxDistSqr)
                    {
                        nodes[i].neighbors.Add(nodes[j]);
                        nodes[j].neighbors.Add(nodes[i]);
                    }
                }
            }
        }
    }

    private static Vector3Int Hash(Vector3 p, float cellSize, bool twoD)
    {
        int x = Mathf.FloorToInt(p.x / cellSize);
        int y = twoD ? 0 : Mathf.FloorToInt(p.y / cellSize);
        int z = Mathf.FloorToInt(p.z / cellSize);
        return new Vector3Int(x, y, z);
    }

    private static List<Vector3Int> BuildNeighborOffsets(bool twoD)
    {
        var offsets = new List<Vector3Int>(twoD ? 9 : 27);
        if (twoD)
        {
            for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                    offsets.Add(new Vector3Int(dx, 0, dz));
        }
        else
        {
            for (int dz = -1; dz <= 1; dz++)
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                        offsets.Add(new Vector3Int(dx, dy, dz));
        }
        return offsets;
    }

    // ===========================================================
    // CHAINED DIJKSTRA (with distance)
    // ===========================================================
    public void FindShortestPath(PointNode start, List<PointNode> destinations)
    {
        if (start == null || destinations == null || destinations.Count == 0)
        {
            Debug.LogWarning("[GraphManager] Start atau destinasi belum dipilih!");
            return;
        }

        if (nodes == null || nodes.Count == 0)
        {
            BuildConnections();
            if (nodes.Count == 0) return;
        }

        Stopwatch sw = null;
        if (logTimings) sw = Stopwatch.StartNew();

        List<PointNode> totalPath = new List<PointNode>();
        float totalDistance = 0f; // >>> DITAMBAHKAN
        PointNode currentStart = start;

        for (int i = 0; i < destinations.Count; i++)
        {
            PointNode currentDest = destinations[i];

            var prev = DijkstraAll_Heap(currentStart);
            var subPath = ReconstructPath(prev, currentStart, currentDest);

            if (subPath.Count > 1)
            {
                // Hitung jarak fisik subpath
                float segmentDistance = CalculatePathDistance(subPath); // >>> DITAMBAHKAN
                totalDistance += segmentDistance;

                if (totalPath.Count > 0)
                    subPath.RemoveAt(0);

                totalPath.AddRange(subPath);
            }
            else
            {
                Debug.LogWarning($"Tidak ada jalur dari {currentStart.name} ke {currentDest.name}");
                break;
            }

            currentStart = currentDest;
        }

        if (logTimings && sw != null)
        {
            sw.Stop();
            Debug.Log($"[GraphManager] Chained Dijkstra selesai ({sw.Elapsed.TotalMilliseconds:F2} ms)");
        }

        if (totalPath.Count > 1)
            DrawPath(totalPath);

        // Simpan untuk UI
        lastTotalDistance = totalDistance; // >>> DITAMBAHKAN
        lastPath = totalPath;             // >>> DITAMBAHKAN

        Debug.Log($"[GraphManager] Total jarak = {totalDistance:F2} meter");
    }

    // ===========================================================
    // HITUNG JARAK FISIK (METER)
    // ===========================================================
    private float CalculatePathDistance(List<PointNode> path)
    {
        float d = 0f;

        for (int i = 0; i < path.Count - 1; i++)
        {
            d += Vector3.Distance(path[i].transform.position, path[i + 1].transform.position);
        }

        return d;
    }

    // ===========================================================
    // DIJKSTRA (HEAP)
    // ===========================================================
    private Dictionary<PointNode, PointNode> DijkstraAll_Heap(PointNode start)
    {
        Stopwatch sw = null;
        if (logTimings) sw = Stopwatch.StartNew();
        int n = nodes.Count;

        var indexOf = new Dictionary<PointNode, int>(n);
        for (int i = 0; i < n; i++) indexOf[nodes[i]] = i;

        if (!indexOf.TryGetValue(start, out int startIdx))
            return new Dictionary<PointNode, PointNode>();

        var positions = new Vector3[n];
        for (int i = 0; i < n; i++)
            positions[i] = nodes[i].transform.position;

        var dist = new float[n];
        var prevIndex = new int[n];
        var visited = new bool[n];

        const float INF = float.PositiveInfinity;

        for (int i = 0; i < n; i++)
        {
            dist[i] = INF;
            prevIndex[i] = -1;
            visited[i] = false;
        }

        dist[startIdx] = 0f;

        var pq = new MinHeap(n);
        pq.Push(startIdx, 0f);

        while (pq.Count > 0)
        {
            var popped = pq.Pop();
            int currentIdx = popped.index;

            if (visited[currentIdx]) continue;
            visited[currentIdx] = true;

            var currentNode = nodes[currentIdx];

            foreach (var neighbor in currentNode.neighbors)
            {
                int nIdx = indexOf[neighbor];
                if (visited[nIdx]) continue;

                float w = (positions[currentIdx] - positions[nIdx]).sqrMagnitude;
                float alt = dist[currentIdx] + w;

                if (alt < dist[nIdx])
                {
                    dist[nIdx] = alt;
                    prevIndex[nIdx] = currentIdx;
                    pq.Push(nIdx, alt);
                }
            }
        }

        var prev = new Dictionary<PointNode, PointNode>();
        for (int i = 0; i < n; i++)
        {
            int p = prevIndex[i];
            if (p >= 0) prev[nodes[i]] = nodes[p];
        }
        
        if (logTimings && sw != null)
        {
            sw.Stop();

            double ms = sw.Elapsed.TotalMilliseconds;
            System.TimeSpan t = sw.Elapsed;

            Debug.Log(
                $"[GraphManager] Dijkstra runtime = " +
                $"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}.{t.Milliseconds:D3} " +
                $"(≈ {ms:F2} ms)"
            );
        }

        return prev;
    }

    // ===========================================================
    // RECONSTRUCT PATH
    // ===========================================================
    private List<PointNode> ReconstructPath(Dictionary<PointNode, PointNode> prev, PointNode start, PointNode end)
    {
        var path = new List<PointNode>();

        if (start == end)
        {
            path.Add(start);
            return path;
        }

        if (!prev.ContainsKey(end)) return path;

        var node = end;

        while (node != null)
        {
            path.Insert(0, node);
            if (node == start) break;

            if (!prev.TryGetValue(node, out node))
            {
                path.Clear();
                break;
            }
        }

        return path;
    }

    // ===========================================================
    // RENDER GARIS
    // ===========================================================
    private void DrawPath(List<PointNode> path)
    {
        if (path == null || path.Count == 0) return;

        if (sharedLineMaterial == null)
        {
            var shader = Shader.Find("Unlit/Color");
            sharedLineMaterial = new Material(shader) { color = Color.cyan };
        }

        GameObject lineObj = new GameObject("PathRenderer");
        lineObj.transform.SetParent(transform);

        var lr = lineObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = path.Count;
        lr.material = sharedLineMaterial;
        lr.widthMultiplier = 0.05f;

        Vector3[] pos = new Vector3[path.Count];
        for (int i = 0; i < path.Count; i++)
            pos[i] = path[i].transform.position;

        lr.SetPositions(pos);
    }

    // ===========================================================
    // CLEAR ALL PATH
    // ===========================================================
    public void ClearAllPaths()
    {
        var toDelete = new List<GameObject>();
        foreach (Transform child in transform)
        {
            if (child.name.Contains("PathRenderer"))
                toDelete.Add(child.gameObject);
        }

        foreach (var obj in toDelete)
            Destroy(obj);
    }

    // ===========================================================
    // MIN-HEAP CLASS
    // ===========================================================
    private struct HeapItem
    {
        public int index;
        public float priority;
        public HeapItem(int idx, float p) { index = idx; priority = p; }
    }

    private class MinHeap
    {
        private readonly List<HeapItem> heap;

        public int Count => heap.Count;

        public MinHeap(int capacity = 0)
        {
            heap = capacity > 0 ? new List<HeapItem>(capacity) : new List<HeapItem>();
        }

        public void Push(int index, float priority)
        {
            heap.Add(new HeapItem(index, priority));
            SiftUp(heap.Count - 1);
        }

        public (int index, float priority) Pop()
        {
            int last = heap.Count - 1;
            var root = heap[0];
            heap[0] = heap[last];
            heap.RemoveAt(last);

            if (heap.Count > 0)
                SiftDown(0);

            return (root.index, root.priority);
        }

        private void SiftUp(int i)
        {
            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                if (heap[i].priority < heap[parent].priority)
                {
                    var tmp = heap[i];
                    heap[i] = heap[parent];
                    heap[parent] = tmp;
                    i = parent;
                }
                else break;
            }
        }

        private void SiftDown(int i)
        {
            int count = heap.Count;
            while (true)
            {
                int left = (i * 2) + 1;
                int right = left + 1;
                int smallest = i;

                if (left < count && heap[left].priority < heap[smallest].priority)
                    smallest = left;

                if (right < count && heap[right].priority < heap[smallest].priority)
                    smallest = right;

                if (smallest == i) break;

                var tmp = heap[i];
                heap[i] = heap[smallest];
                heap[smallest] = tmp;

                i = smallest;
            }
        }
    }
}
