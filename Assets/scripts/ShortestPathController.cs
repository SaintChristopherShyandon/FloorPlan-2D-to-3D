using UnityEngine;
using System.Collections.Generic;

public class ShortestPathController : MonoBehaviour
{
    public GraphManager graphManager;
    private PointNode startNode = null;
    private List<PointNode> destinationNodes = new List<PointNode>();

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                PointNode node = hit.collider.GetComponent<PointNode>();
                if (node != null)
                    HandleClick(node);
            }
        }
    }

    void HandleClick(PointNode node)
    {
        if (startNode == null)
        {
            startNode = node;
            node.SetAsStart();
        }
        else if (!destinationNodes.Contains(node) && node != startNode)
        {
            destinationNodes.Add(node);
            node.SetAsDestination();
        }
    }

    public void OnClickFindPath()
    {
        if (startNode == null || destinationNodes.Count == 0)
        {
            Debug.LogWarning("Start atau tujuan belum dipilih!");
            return;
        }

        graphManager.BuildConnections(); // rebuild agar koneksi selalu up-to-date
        graphManager.FindShortestPath(startNode, destinationNodes);
    }

    public void OnClickReset()
    {
        graphManager.ClearAllPaths();

        foreach (var node in FindObjectsOfType<PointNode>())
            node.ResetSelection();

        startNode = null;
        destinationNodes.Clear();
    }
}
