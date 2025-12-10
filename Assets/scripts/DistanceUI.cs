using UnityEngine;
using TMPro;

public class DistanceUI : MonoBehaviour
{
    [Header("Reference")]
    public GraphManager graph;

    [Header("UI Text (TMP)")]
    public TMP_Text distanceText;

    private void Update()
    {
        if (graph == null || distanceText == null)
            return;

        distanceText.text = $"Total Jarak: {graph.lastTotalDistance:F2} m";
    }
}
