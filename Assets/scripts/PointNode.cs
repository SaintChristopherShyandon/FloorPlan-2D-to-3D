using UnityEngine;
using System.Collections.Generic;

public class PointNode : MonoBehaviour
{
    public List<PointNode> neighbors = new List<PointNode>();
    public bool isStart = false;
    public bool isDestination = false;

    private Renderer rend;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
    }

    private void OnMouseDown()
    {
        // Toggle visual (renderer + sphere mesh)
        if (rend == null)
        {
            // Tambahkan sphere visual
            meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere).GetComponent<MeshFilter>().sharedMesh;

            rend = gameObject.AddComponent<MeshRenderer>();

            // Tambahkan collider biar bisa diklik
            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.radius = 0.1f; // kamu bisa ubah 0.1 → 0.15 kalau tembok besar

            // Tentukan warna berdasarkan parent
            string parentName = transform.parent != null ? transform.parent.name.ToLower() : "";

            if (parentName.Contains("roof") || parentName.Contains("floor"))
            {
                rend.material.color = Color.yellow;
            }
            else if (parentName.Contains("wall"))
            {
                rend.material.color = Color.red;
            }
            else
            {
                rend.material.color = Color.white;
            }
        }
        else
        {
            // Hapus visualnya
            if (meshFilter != null) Destroy(meshFilter);
            if (rend != null) Destroy(rend);
            if (meshCollider != null) Destroy(meshCollider);
            meshFilter = null;
            rend = null;
            meshCollider = null;
        }
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
