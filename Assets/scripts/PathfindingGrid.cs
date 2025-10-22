using UnityEngine;
using System.Collections.Generic;

// Kelas untuk merepresentasikan satu titik/node dalam grid
public class Node
{
    public Vector3 worldPosition;
    public List<Node> neighbors;
    public Node parent; // Untuk melacak jalur kembali
    public float gCost; // Jarak dari node awal

    public Node(Vector3 _worldPos)
    {
        worldPosition = _worldPos;
        neighbors = new List<Node>();
        gCost = float.MaxValue;
    }
}

public class PathfindingGrid : MonoBehaviour
{
    public static PathfindingGrid Instance { get; private set; }

    public float nodeSpacing = 0.5f; // Jarak antar node. Sesuaikan sesuai kebutuhan.
    private List<Node> grid = new List<Node>();
    private Bounds buildingBounds;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // Panggil fungsi ini SETELAH semua lantai dibuat oleh Builder.cs
    public void GenerateGrid()
    {
        Debug.Log("Generating pathfinding grid...");
        CalculateBuildingBounds();
        CreateGrid();
        ConnectNodes();
        Debug.Log($"Grid generated with {grid.Count} nodes.");
    }

    void CalculateBuildingBounds()
    {
        buildingBounds = new Bounds();
        GameObject[] floors = GameObject.FindGameObjectsWithTag("FloorContainer"); // Kita akan tag kontainer lantai nanti

        if (floors.Length == 0) {
            Debug.LogError("No objects with tag 'FloorContainer' found. Cannot calculate bounds.");
            // Fallback jika tidak ada kontainer lantai, cari semua renderer
            Renderer[] allRenderers = FindObjectsOfType<Renderer>();
            if(allRenderers.Length == 0) return;

            buildingBounds = allRenderers[0].bounds;
            foreach (Renderer r in allRenderers)
            {
                buildingBounds.Encapsulate(r.bounds);
            }
        } else {
             buildingBounds = new Bounds(floors[0].transform.position, Vector3.zero);
             foreach(GameObject floor in floors) {
                Renderer[] renderers = floor.GetComponentsInChildren<Renderer>();
                foreach(Renderer r in renderers) {
                    buildingBounds.Encapsulate(r.bounds);
                }
             }
        }
       
        // Beri sedikit padding agar node tidak terlalu mepet
        buildingBounds.Expand(nodeSpacing * 2);
    }

    void CreateGrid()
    {
        grid.Clear();
        for (float x = buildingBounds.min.x; x < buildingBounds.max.x; x += nodeSpacing)
        {
            for (float y = buildingBounds.min.y; y < buildingBounds.max.y; y += nodeSpacing)
            {
                for (float z = buildingBounds.min.z; z < buildingBounds.max.z; z += nodeSpacing)
                {
                    grid.Add(new Node(new Vector3(x, y, z)));
                }
            }
        }
    }

    void ConnectNodes()
    {
        foreach (Node node in grid)
        {
            foreach (Node potentialNeighbor in grid)
            {
                if (node == potentialNeighbor) continue;

                // Cek jarak untuk menentukan tetangga (koneksi 6 arah: atas,bawah,kiri,kanan,depan,belakang)
                if (Vector3.Distance(node.worldPosition, potentialNeighbor.worldPosition) <= nodeSpacing + 0.01f)
                {
                    node.neighbors.Add(potentialNeighbor);
                }
            }
        }
    }
    
    // Fungsi untuk mendapatkan node terdekat dari sebuah posisi di dunia
    public Node GetNodeFromWorldPoint(Vector3 worldPoint)
    {
        Node closestNode = null;
        float minDistance = float.MaxValue;

        foreach (Node node in grid)
        {
            float distance = Vector3.Distance(worldPoint, node.worldPosition);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestNode = node;
            }
        }
        return closestNode;
    }

    // (Opsional) Visualisasikan grid untuk debugging
    void OnDrawGizmos()
    {
        if (grid != null && grid.Count > 0)
        {
            Gizmos.color = new Color(0, 1, 0, 0.1f);
            foreach (Node n in grid)
            {
                Gizmos.DrawSphere(n.worldPosition, nodeSpacing / 10);
            }
             Gizmos.color = Color.yellow;
             Gizmos.DrawWireCube(buildingBounds.center, buildingBounds.size);
        }
    }
}