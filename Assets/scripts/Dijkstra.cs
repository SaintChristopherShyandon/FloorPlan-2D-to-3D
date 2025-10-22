using UnityEngine;
using System.Collections.Generic;

public static class Dijkstra
{
    public static List<Node> FindShortestPath(PathfindingGrid grid, Node startNode, Node endNode)
    {
        // Reset semua node
        var allNodes = grid.GetComponentsInChildren<Node>(); // Ini salah, grid bukan GO. Harusnya ada list node di grid.
        // Kita asumsikan PathfindingGrid.Instance.grid bisa diakses atau kita passing list-nya
        // Untuk sementara kita tidak reset di sini, tapi di manager sebelum memanggil.

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        startNode.gCost = 0;
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].gCost < currentNode.gCost)
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == endNode)
            {
                return RetracePath(startNode, endNode);
            }

            foreach (Node neighbor in currentNode.neighbors)
            {
                if (closedSet.Contains(neighbor)) continue;

                float newMovementCostToNeighbor = currentNode.gCost + Vector3.Distance(currentNode.worldPosition, neighbor.worldPosition);
                if (newMovementCostToNeighbor < neighbor.gCost)
                {
                    neighbor.gCost = newMovementCostToNeighbor;
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return null; // Tidak ditemukan jalur
    }

    private static List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Add(startNode);
        path.Reverse();
        return path;
    }
}