using UnityEngine;
using System.Collections.Generic;

public class PointNode : MonoBehaviour
{
    [Header("Connections")]
    public List<PointNode> neighbors = new List<PointNode>();

    // HAPUS STATIC VARIABLES. Biarkan Controller yang mengaturnya.
    // Variabel state hanya untuk visualisasi diri sendiri
    private bool _isStart = false;
    private bool _isDestination = false;

    private Renderer rend;
    private MeshFilter meshFilter;
    private Color originalColor = Color.white; // Simpan warna asli

    private void Awake()
    {
        // Pastikan ada collider
        if (!TryGetComponent(out Collider col))
        {
            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.radius = 0.5f; // Radius diperbesar agar mudah diklik
            sc.isTrigger = true;
        }
        
        // Setup referensi renderer jika sudah ada visual
        // (Visual sphere biasanya dibuat runtime, jadi kita handle nanti)
    }

    // Fungsi visualisasi dipanggil oleh Controller
    public void SetVisualState(bool isStart, bool isDestination)
    {
        _isStart = isStart;
        _isDestination = isDestination;

        // Pastikan visual sphere ada
        if (rend == null) CreateVisuals();

        if (_isStart)
            rend.material.color = Color.green;
        else if (_isDestination)
            rend.material.color = Color.red;
        else
            rend.material.color = originalColor; // Kembali ke warna tipe (kuning/merah/putih)
    }

    public void ResetNode()
    {
        SetVisualState(false, false);
        // Opsi: Jika ingin menyembunyikan sphere saat tidak dipilih, panggil HideVisuals() di sini
    }

    private void CreateVisuals()
    {
        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");

        if (rend == null) rend = gameObject.AddComponent<MeshRenderer>();
        
        // Tentukan warna dasar berdasarkan parent (Wall/Floor)
        string parentName = transform.parent != null ? transform.parent.name.ToLower() : "";
        if (parentName.Contains("roof") || parentName.Contains("floor"))
            originalColor = Color.yellow;
        else if (parentName.Contains("wall"))
            originalColor = Color.red; // Wall points
        else
            originalColor = Color.white;
            
        rend.material.color = originalColor;
    }
}