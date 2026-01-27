using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointCalculator : MonoBehaviour
{
    // Metode untuk menghitung total points
    public void CalculateTotalPoints()
    {
        // Cari semua GameObject dengan tag "point"
        GameObject[] allPoints = GameObject.FindGameObjectsWithTag("point");
        int totalPoints = allPoints.Length;

        // Opsional: Hitung per kategori (floor/roof vs wall)
        int floorRoofPoints = 0;
        int wallPoints = 0;

        foreach (GameObject point in allPoints)
        {
            // Asumsikan parent adalah floor/roof atau wall
            Transform parent = point.transform.parent;
            if (parent != null)
            {
                if (parent.name.Contains("Floor") || parent.name.Contains("Roof"))
                {
                    floorRoofPoints++;
                }
                else if (parent.name.Contains("wall"))
                {
                    wallPoints++;
                }
            }
        }

        // Log hasil
        Debug.Log($"[PointCalculator] Total Points: {totalPoints} (Floor/Roof: {floorRoofPoints}, Wall: {wallPoints})");

        // Opsional: Jika ingin reset atau aksi lain, tambahkan di sini
    }

    // Contoh: Panggil otomatis di Start (hapus jika tidak perlu)
    void Start()
    {
        // Uncomment baris di bawah jika ingin kalkulasi otomatis saat scene start
        // CalculateTotalPoints();
    }
}