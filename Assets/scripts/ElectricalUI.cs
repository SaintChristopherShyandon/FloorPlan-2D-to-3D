using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ElectricalUI : MonoBehaviour
{
    public GameObject pathfindingUIPanel; // Panel yang berisi semua UI pathfinding
    public PathfindingManager pathfindingManager;
    public List<Toggle> floorToggles; // Assign di inspector
    public List<GameObject> floorContainers; // Assign di inspector

    void Start()
    {
        // Pastikan panel disembunyikan pada awalnya
        pathfindingUIPanel.SetActive(false);
        
        // Setup listener untuk setiap toggle
        for (int i = 0; i < floorToggles.Count; i++)
        {
            int index = i; // Penting untuk menghindari masalah closure di lambda
            floorToggles[i].onValueChanged.AddListener((isOn) => OnFloorToggleChanged(index, isOn));
            // Inisialisasi visibilitas lantai berdasarkan toggle awal
             if (i < floorContainers.Count) {
                floorContainers[i].SetActive(floorToggles[i].isOn);
            }
        }
    }
    
    // Fungsi untuk membuka/menutup menu pathfinding
    public void TogglePathfindingMenu()
    {
        bool isActive = !pathfindingUIPanel.activeSelf;
        pathfindingUIPanel.SetActive(isActive);

        // Jika menu ditutup, pastikan untuk membersihkan pathfinding
        if (!isActive)
        {
            pathfindingManager.ClearAll();
        }
    }
    
    private void OnFloorToggleChanged(int floorIndex, bool isVisible)
    {
        if (floorIndex < floorContainers.Count)
        {
            floorContainers[floorIndex].SetActive(isVisible);
        }
    }
}