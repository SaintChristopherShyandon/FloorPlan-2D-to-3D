using UnityEngine;
using System.Collections.Generic;

public class PointNode : MonoBehaviour
{
    public List<PointNode> neighbors = new List<PointNode>();
    public bool isStart = false;
    public bool isDestination = false;

    private Renderer rend;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        UpdateColor();
    }

    public void SetAsStart()
    {
        isStart = true;
        isDestination = false;
        UpdateColor();
    }

    public void SetAsDestination()
    {
        isDestination = true;
        isStart = false;
        UpdateColor();
    }

    public void ResetSelection()
    {
        isStart = false;
        isDestination = false;
        UpdateColor();
    }

    private void UpdateColor()
    {
        if (rend == null) return;

        if (isStart)
            rend.material.color = Color.green;
        else if (isDestination)
            rend.material.color = Color.red;
        else
            rend.material.color = Color.yellow;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        foreach (var n in neighbors)
        {
            if (n != null)
                Gizmos.DrawLine(transform.position, n.transform.position);
        }
    }
}
