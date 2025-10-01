using UnityEngine;

public class ClickMarker : MonoBehaviour
{
    public Transform startMarker;
    public Transform goalMarker;
    bool settingStart = true;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (settingStart) startMarker.position = hit.point;
                else goalMarker.position = hit.point;

                settingStart = !settingStart;
                Debug.Log($"📍 Marker {(settingStart ? "Start" : "Goal")} dipindahkan.");
            }
        }
    }
}
