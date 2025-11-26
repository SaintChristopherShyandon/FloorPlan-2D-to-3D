using UnityEngine;
using System.Collections.Generic;

public class PointNode : MonoBehaviour
{
    [Header("Connections")]
    public List<PointNode> neighbors = new List<PointNode>();

    [Header("State")]
    public bool isStart = false;
    public bool isDestination = false;

    // Komponen Visual (disimpan agar bisa dihapus nanti)
    private Renderer rend;
    private MeshFilter meshFilter;

    // --- FUNGSI UTAMA YANG DIPANGGIL CLICK MANAGER ---
    public void TogglePoint()
    {
        // Cek apakah visual sudah ada?
        if (rend == null)
        {
            ShowVisuals();
        }
        else
        {
            HideVisuals();
        }
    }

    private void ShowVisuals()
    {
        // 1. Tambahkan MeshFilter (Bentuk Bola)
        if (meshFilter == null) 
            meshFilter = gameObject.AddComponent<MeshFilter>();
        
        meshFilter.sharedMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere).GetComponent<MeshFilter>().sharedMesh;

        // 2. Tambahkan Renderer (Warna)
        if (rend == null) 
            rend = gameObject.AddComponent<MeshRenderer>();

        // 3. Update warna sesuai status (Start/End/Normal)
        UpdateColor();
    }

    private void HideVisuals()
    {
        // Hapus komponen visual agar kembali invisible
        if (meshFilter != null) Destroy(meshFilter);
        if (rend != null) Destroy(rend);

        meshFilter = null;
        rend = null;
        
        // Reset status jika di-hide (Opsional, tergantung kebutuhanmu)
        // ResetSelection(); 
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
        // Jika belum ada renderer (sedang invisible), tidak perlu ubah warna
        if (rend == null) return;

        if (isStart)
        {
            rend.material.color = Color.green;
        }
        else if (isDestination)
        {
            rend.material.color = Color.red;
        }
        else
        {
            // Warna Default berdasarkan Parent (Wall/Floor/Roof)
            string parentName = transform.parent != null ? transform.parent.name.ToLower() : "";

            if (parentName.Contains("roof") || parentName.Contains("floor"))
            {
                rend.material.color = Color.yellow;
            }
            else if (parentName.Contains("wall"))
            {
                // Bisa ubah jadi merah atau putih sesuai selera
                rend.material.color = Color.red; 
            }
            else
            {
                rend.material.color = Color.white;
            }
        }
    }

    // Visualisasi garis koneksi di Scene View (Editor Only)
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