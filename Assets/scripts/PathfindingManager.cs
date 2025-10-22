using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class PathfindingManager : MonoBehaviour
{
    public Camera mainCamera;
    public LayerMask clickLayer; // Layer untuk dinding, lantai, dll.
    public GameObject startPointPrefab; // Prefab untuk menandai titik awal
    public GameObject endPointPrefab;   // Prefab untuk menandai titik tujuan
    public LineRenderer lineRenderer;   // Komponen untuk menggambar jalur

    private Node startNode;
    private List<Node> endNodes = new List<Node>();

    private List<GameObject> pointMarkers = new List<GameObject>();
    private bool isSettingStart = false;
    private bool isSettingEnd = false;

    void Update()
    {
        // Cek jika user klik mouse dan tidak sedang di atas UI
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            if (isSettingStart || isSettingEnd)
            {
                HandlePointPlacement();
            }
        }
    }

    private void HandlePointPlacement()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, clickLayer))
        {
            Node selectedNode = PathfindingGrid.Instance.GetNodeFromWorldPoint(hit.point);
            if (selectedNode == null) return;

            if (isSettingStart)
            {
                // Hapus marker start lama jika ada
                if(startNode != null) {
                    var oldMarker = GameObject.FindGameObjectWithTag("StartMarker");
                    if (oldMarker) Destroy(oldMarker);
                }
                
                startNode = selectedNode;
                GameObject marker = Instantiate(startPointPrefab, hit.point, Quaternion.identity);
                marker.tag = "StartMarker";
                pointMarkers.Add(marker);
                isSettingStart = false; // Matikan mode setelah memilih
                Debug.Log("Start point set.");
            }
            else if (isSettingEnd)
            {
                endNodes.Add(selectedNode);
                GameObject marker = Instantiate(endPointPrefab, hit.point, Quaternion.identity);
                pointMarkers.Add(marker);
                // Biarkan mode setting end tetap aktif untuk menambah beberapa titik
                Debug.Log("End point added.");
            }
        }
    }

    // --- FUNGSI UNTUK DIPANGGIL DARI UI ---

    public void EnterSetStartMode()
    {
        isSettingStart = true;
        isSettingEnd = false;
        Debug.Log("Mode: Set Start Point. Click on a surface.");
    }

    public void EnterSetEndMode()
    {
        isSettingStart = false;
        isSettingEnd = true;
        Debug.Log("Mode: Set End Point(s). Click on one or more surfaces.");
    }


    public void CalculateAndDrawPath()
    {
        if (startNode == null || endNodes.Count == 0)
        {
            Debug.LogError("Start point or End points not set!");
            return;
        }
        
        // Matikan mode pemilihan
        isSettingEnd = false;
        isSettingStart = false;

        List<Vector3> finalPathPositions = new List<Vector3>();
        Node currentStart = startNode;

        // Iterasi untuk setiap titik tujuan
        foreach (Node endNode in endNodes)
        {
            // Reset node costs sebelum setiap pencarian
            // (Ini harusnya ada di dalam PathfindingGrid atau dilakukan manual)
            // Untuk simple, kita asumsikan grid fresh. Implementasi lebih robust perlu reset.

            List<Node> segment = Dijkstra.FindShortestPath(PathfindingGrid.Instance, currentStart, endNode);
            if (segment != null)
            {
                foreach (var node in segment)
                {
                    finalPathPositions.Add(node.worldPosition);
                }
                currentStart = endNode; // Titik akhir segmen ini menjadi titik awal segmen berikutnya
            }
            else
            {
                Debug.LogWarning($"Path not found to one of the destinations.");
            }
        }

        // Gambar jalur
        lineRenderer.positionCount = finalPathPositions.Count;
        lineRenderer.SetPositions(finalPathPositions.ToArray());
        lineRenderer.enabled = true;
    }

    public void ClearAll()
    {
        startNode = null;
        endNodes.Clear();
        
        foreach (var marker in pointMarkers)
        {
            Destroy(marker);
        }
        pointMarkers.Clear();

        lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;
        
        isSettingStart = false;
        isSettingEnd = false;
        Debug.Log("All points and path cleared.");
    }
}