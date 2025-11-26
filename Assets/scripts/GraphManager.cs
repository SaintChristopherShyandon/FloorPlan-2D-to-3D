using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics; // untuk Stopwatch
using Debug = UnityEngine.Debug;

public class GraphManager : MonoBehaviour
{
    [Header("Graph")]
    public float maxDistance = 2.0f; // jarak maksimum antar node yang dianggap terhubung
    public bool use2D = false;       // jika true, hash grid abaikan sumbu Y (cocok untuk game 2D/topdown)
    public bool logTimings = true;

    private List<PointNode> nodes = new List<PointNode>();
    private static Material sharedLineMaterial; // hindari alokasi material per path

    void Start()
    {
        BuildConnections(); // bisa dipanggil ulang saat layout node berubah
    }

    // ---------------------------------------------------------------
    // MEMBANGUN GRAPH MENGGUNAKAN SPATIAL HASH (skala besar)
    // ---------------------------------------------------------------
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

    // Grid hash: cellSize = maxDistance, cek hanya sel sekitar
    private static void BuildConnectionsSpatialHash(List<PointNode> nodes, float cellSize, bool twoD)
    {
        int n = nodes.Count;
        float maxDistSqr = cellSize * cellSize;

        // Precompute posisi & index mapping agar akses cepat
        var positions = new Vector3[n];
        var indexOf = new Dictionary<PointNode, int>(n);
        for (int i = 0; i < n; i++)
        {
            positions[i] = nodes[i].transform.position;
            indexOf[nodes[i]] = i;
        }

        // Map: cell -> list of indices node pada cell tsb
        var grid = new Dictionary<Vector3Int, List<int>>(n);

        // Masukkan semua node ke dalam sel grid
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

        // Precompute offset sel yang perlu dicek
        var offsets = BuildNeighborOffsets(twoD);

        // Buat edge hanya sekali per pasangan (gunakan id i < j)
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
                    if (j <= i) continue; // hindari duplikasi dan self

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
            // 3x3 di bidang XZ; Y konstan 0
            for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                    offsets.Add(new Vector3Int(dx, 0, dz));
        }
        else
        {
            // 3x3x3 di XYZ
            for (int dz = -1; dz <= 1; dz++)
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                        offsets.Add(new Vector3Int(dx, dy, dz));
        }
        return offsets;
    }

    // ---------------------------------------------------------------
    // CARI JALUR TERPENDEK MENGGUNAKAN DIJKSTRA (heap)
    // ---------------------------------------------------------------
// ---------------------------------------------------------------
// CARI JALUR TERPENDEK BERANTAI (CHAINED DIJKSTRA)
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
        if (nodes.Count == 0) return;
    }

    Stopwatch sw = null;
    if (logTimings) sw = Stopwatch.StartNew();

    // Simpan semua path yang berhasil ditemukan
    List<PointNode> totalPath = new List<PointNode>();
    PointNode currentStart = start;

    for (int i = 0; i < destinations.Count; i++)
    {
        PointNode currentDest = destinations[i];
        Debug.Log($"[GraphManager] Mencari jalur dari {currentStart.name} ke {currentDest.name}...");

        var prev = DijkstraAll_Heap(currentStart);
        var subPath = ReconstructPath(prev, currentStart, currentDest);

        if (subPath.Count > 1)
        {
            // Gabungkan ke path total
            if (totalPath.Count > 0)
            {
                // Hindari duplikasi titik penghubung (start = end sebelumnya)
                subPath.RemoveAt(0);
            }
            totalPath.AddRange(subPath);

            // Gambar path kecilnya untuk debugging
            DrawPath(subPath);

            Debug.Log($"[GraphManager] Jalur ditemukan dari {currentStart.name} ke {currentDest.name} ({subPath.Count} titik)");
        }
        else
        {
            Debug.LogWarning($"[GraphManager] Tidak ada jalur dari {currentStart.name} ke {currentDest.name}");
            break;
        }

        // Update titik awal berikutnya
        currentStart = currentDest;
    }

    if (logTimings && sw != null)
    {
        sw.Stop();
        Debug.Log($"[GraphManager] Chained Dijkstra selesai. Total waktu = {sw.Elapsed.TotalMilliseconds:F2} ms");
    }

    // Gambar path total gabungan (opsional)
    if (totalPath.Count > 1)
    {
        DrawPath(totalPath);
        Debug.Log($"[GraphManager] Total jalur berantai ({totalPath.Count} titik) digambar.");
    }
}


    // Dijkstra memakai min-heap; bobot edge = jarak kuadrat (tanpa sqrt, cepat)
    // NOTE: Jika ingin jarak fisik, ganti perhitungan 'alt' menjadi:
    // float alt = dist[currentIdx] + Vector3.Distance(positions[currentIdx], positions[nIdx]);
    private Dictionary<PointNode, PointNode> DijkstraAll_Heap(PointNode start)
    {
        int n = nodes.Count;

        // Mapping index untuk akses cepat
        var indexOf = new Dictionary<PointNode, int>(n);
        for (int i = 0; i < n; i++) indexOf[nodes[i]] = i;

        if (!indexOf.TryGetValue(start, out int startIdx))
        {
            Debug.LogWarning("[GraphManager] Start node tidak ada di graph.");
            return new Dictionary<PointNode, PointNode>(0);
        }

        // Cache posisi agar akses transform tidak berulang
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

        // Min-heap priority queue
        var pq = new MinHeap(n);
        pq.Push(startIdx, 0f);

        while (pq.Count > 0)
        {
            var popped = pq.Pop();
            int currentIdx = popped.index;

            if (visited[currentIdx]) continue; // skip entri usang
            visited[currentIdx] = true;

            // Relax semua tetangga
            var currentNode = nodes[currentIdx];
            var neighbors = currentNode.neighbors;

            for (int k = 0; k < neighbors.Count; k++)
            {
                var neighbor = neighbors[k];
                int nIdx = indexOf[neighbor];
                if (visited[nIdx]) continue;

                // Bobot edge = jarak kuadrat antar node
                float w = (positions[currentIdx] - positions[nIdx]).sqrMagnitude;
                float alt = dist[currentIdx] + w;

                if (alt < dist[nIdx])
                {
                    dist[nIdx] = alt;
                    prevIndex[nIdx] = currentIdx;
                    pq.Push(nIdx, alt); // tidak perlu decrease-key; push ulang saja
                }
            }
        }

        // Bangun dictionary prev (PointNode -> PointNode)
        var prev = new Dictionary<PointNode, PointNode>(n);
        for (int i = 0; i < n; i++)
        {
            int p = prevIndex[i];
            if (p >= 0)
                prev[nodes[i]] = nodes[p];
        }

        return prev;
    }

    // ---------------------------------------------------------------
    // MEMBANGUN URUTAN NODE DARI HASIL DIJKSTRA
    // ---------------------------------------------------------------
    private List<PointNode> ReconstructPath(Dictionary<PointNode, PointNode> prev, PointNode start, PointNode end)
    {
        var path = new List<PointNode>();

        if (end == null) return path;
        if (start == end)
        {
            path.Add(start);
            return path;
        }

        // Jika tidak ada predecessor utk end
        if (!prev.ContainsKey(end))
            return path; // tidak ada jalur

        // Telusuri mundur dari end ke start
        var node = end;
        while (node != null)
        {
            path.Insert(0, node);
            if (node == start) break;
            if (!prev.TryGetValue(node, out node))
            {
                // putus; tidak ada jalur lengkap
                path.Clear();
                break;
            }
        }

        return path;
    }

    // ---------------------------------------------------------------
    // GAMBAR GARIS (JALUR) - gunakan shared material untuk hindari GC
    // ---------------------------------------------------------------
    private void DrawPath(List<PointNode> path)
    {
        if (path == null || path.Count == 0) return;

        if (sharedLineMaterial == null)
        {
            // Catatan: Pastikan shader tersedia. Untuk URP/HDRP, sesuaikan shader/material.
            var shader = Shader.Find("Unlit/Color");
            sharedLineMaterial = new Material(shader) { color = Color.cyan };
        }

        GameObject lineObj = new GameObject($"Path_{path[0].name}_to_{path[path.Count - 1].name}");
        lineObj.transform.SetParent(transform, false);

        var lr = lineObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = path.Count;
        lr.material = sharedLineMaterial; // shared, tidak buat material baru
        lr.widthMultiplier = 0.05f;
        lr.numCornerVertices = 2;
        lr.numCapVertices = 2;

        // Jika ingin warna per-path tanpa instancing material,
        // bisa gunakan Gradient color pada LineRenderer:
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.cyan, 0f), new GradientColorKey(Color.cyan, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );
        lr.colorGradient = grad;

        var positions = new Vector3[path.Count];
        for (int i = 0; i < path.Count; i++)
            positions[i] = path[i].transform.position;

        lr.SetPositions(positions);
    }

    // ---------------------------------------------------------------
    // RESET SEMUA GARIS
    // ---------------------------------------------------------------
    public void ClearAllPaths()
    {
        // Hapus semua anak yang namanya diawali "Path_"
        var toDelete = new List<GameObject>();
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Path_"))
                toDelete.Add(child.gameObject);
        }

        for (int i = 0; i < toDelete.Count; i++)
            Destroy(toDelete[i]);

        Debug.Log("[GraphManager] Semua garis path dihapus.");
    }

    // ---------------------------------------------------------------
    // MIN-HEAP PRIORITY QUEUE (untuk Dijkstra)
    // ---------------------------------------------------------------
    // Simpan pasangan (index node, priority)
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

        public void Clear() => heap.Clear();

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
                    Swap(i, parent);
                    i = parent;
                }
                else
                {
                    break;
                }
            }
        }

        private void SiftDown(int i)
        {
            int count = heap.Count;
            while (true)
            {
                int left = (i << 1) + 1;
                int right = left + 1;
                int smallest = i;

                if (left < count && heap[left].priority < heap[smallest].priority)
                    smallest = left;

                if (right < count && heap[right].priority < heap[smallest].priority)
                    smallest = right;

                if (smallest == i) break;

                Swap(i, smallest);
                i = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            var tmp = heap[a];
            heap[a] = heap[b];
            heap[b] = tmp;
        }
    }
}
