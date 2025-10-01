using UnityEngine;
using System.Collections.Generic;

public class ClickPointSelector : MonoBehaviour
{
    public List<Transform> goalMarkers = new List<Transform>();
    public Transform startMarkerPrefab;
    public Transform goalMarkerPrefab;

    private Transform startMarker;

    public void ResetPoints()
    {
        if (startMarker != null) Destroy(startMarker.gameObject);
        foreach (var g in goalMarkers) Destroy(g.gameObject);
        goalMarkers.Clear();
    }

    public void SetStartPoint()
    {
        StartCoroutine(WaitForClick(isStart: true));
    }

    public void AddGoalPoint()
    {
        StartCoroutine(WaitForClick(isStart: false));
    }

    IEnumerator<WaitForEndOfFrame> WaitForClick(bool isStart)
    {
        Debug.Log("🖱️ Click on the floor to set " + (isStart ? "Start" : "Goal"));
        bool done = false;

        while (!done)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (isStart)
                    {
                        if (startMarker != null) Destroy(startMarker.gameObject);
                        startMarker = Instantiate(startMarkerPrefab, hit.point, Quaternion.identity);
                    }
                    else
                    {
                        var newGoal = Instantiate(goalMarkerPrefab, hit.point, Quaternion.identity);
                        goalMarkers.Add(newGoal);
                    }
                    done = true;
                }
            }
            yield return new WaitForEndOfFrame();
        }
    }

    public Transform GetStartMarker() => startMarker;
}
