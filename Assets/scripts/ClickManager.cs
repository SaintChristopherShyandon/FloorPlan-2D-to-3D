using UnityEngine;

public class ClickManager : MonoBehaviour
{
    [Header("Seberapa meleset boleh ngeklik?")]
    public float clickRadius = 0.5f; // Semakin besar, semakin mudah kliknya (walau meleset)
    public LayerMask pointLayer; // Opsional: untuk filter layer

    void Update()
    {
        // Deteksi Klik Kiri Mouse
        if (Input.GetMouseButtonDown(0))
        {
            DetectPointClick();
        }
    }

    void DetectPointClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;

        // KUNCI RAHASIANYA DISINI: SphereCast
        // Ini seperti menembakkan bola tenis, bukan jarum. Jadi gampang kena.
        if (Physics.SphereCast(ray, clickRadius, out hitInfo))
        {
            // Cek apakah yang kena tembak punya script PointNode?
            PointNode node = hitInfo.collider.GetComponent<PointNode>();
            
            // Jika kena PointNode, atau mungkin kena visual sphere-nya PointNode
            if (node == null)
            {
                 // Coba cari di parent atau object itu sendiri
                 node = hitInfo.collider.gameObject.GetComponentInParent<PointNode>();
            }

            // Eksekusi
            if (node != null)
            {
                node.TogglePoint();
            }
        }
    }
    
    // Untuk visualisasi radius klik di Scene View (biar kamu bisa lihat seberapa besar bolanya)
    private void OnDrawGizmos()
    {
        // Hanya visualisasi
    }
}