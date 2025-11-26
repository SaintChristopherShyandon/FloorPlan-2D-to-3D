using UnityEngine;
using System.Collections.Generic;

public class PointNode : MonoBehaviour
{
    [Header("Connections")]
    public List<PointNode> neighbors = new List<PointNode>();

    [Header("State")]
    public bool isStart = false;
    public bool isDestination = false;

    // GLOBAL STATIC (tanpa PointSelector)
    public static PointNode currentStart = null;
    public static PointNode currentDestination = null;

    // Komponen visual
    private Renderer rend;
    private MeshFilter meshFilter;

    private void Awake()
    {
        // Collider wajib ada untuk SphereCast
        if (!TryGetComponent(out Collider col))
        {
            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.radius = 0.15f;
        }
    }

    // Called by ClickManager
    public void OnClicked()
    {
        // === 1. Atur START ===
        if (currentStart == null)
        {
            currentStart = this;
            SetAsStart();
            ShowVisuals();
            return;
        }

        // === 2. Atur DESTINATION ===
        if (currentStart != null && currentDestination == null && this != currentStart)
        {
            currentDestination = this;
            SetAsDestination();
            ShowVisuals();
            return;
        }

        // === 3. Kalau start & destination sudah ada → hanya toggle visual ===
        ToggleVisual();
    }

    private void ToggleVisual()
    {
        if (rend == null)
            ShowVisuals();
        else
            HideVisuals();
    }

    private void ShowVisuals()
    {
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");

        if (rend == null)
            rend = gameObject.AddComponent<MeshRenderer>();

        UpdateColor();
    }

    private void HideVisuals()
    {
        if (meshFilter != null) Destroy(meshFilter);
        if (rend != null) Destroy(rend);

        meshFilter = null;
        rend = null;
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
        {
            string parentName = transform.parent != null ? transform.parent.name.ToLower() : "";
            if (parentName.Contains("roof") || parentName.Contains("floor"))
                rend.material.color = Color.yellow;
            else if (parentName.Contains("wall"))
                rend.material.color = Color.red;
            else
                rend.material.color = Color.white;
        }
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
