using UnityEngine;

public class ClickManager : MonoBehaviour
{
    public float clickRadius = 0.5f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            DetectPointClick();
    }

    void DetectPointClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;

        if (Physics.SphereCast(ray, clickRadius, out hitInfo))
        {
            PointNode node = hitInfo.collider.GetComponent<PointNode>();
            if (node == null)
                node = hitInfo.collider.GetComponentInParent<PointNode>();

            if (node != null)
                node.OnClicked();   // 🔥 PANGGIL INI SAJA
        }
    }
}
