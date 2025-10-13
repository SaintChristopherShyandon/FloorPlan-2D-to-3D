using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Builder : MonoBehaviour
{
    // Tinggi per lantai dalam unit Unity (misalnya 4 meter)
    private const float FLOOR_HEIGHT = 4.0f;

    // Data statis global
    public static float xScale;
    public static float yScale;
    public static float originalScale;
    public static string data;

    // Objek spawner (jika nanti ingin dipakai)
    [Header("Optional Spawner Reference")]
    public GameObject spwaner;

    // =======================================================================
    //  DATA KELAS UNTUK JSON PARSING (disesuaikan dengan format API baru)
    // =======================================================================

    [Serializable]
    public class Point
    {
        public double x1;
        public double y1;
        public double x2;
        public double y2;
    }

    [Serializable]
    public class NamesSub
    {
        public string name;
    }

    [Serializable]
    public class FloorData
    {
        public int floor_index;
        public Point[] points;
        public NamesSub[] classes;
        public int Width;
        public int Height;
        public float averageDoor;
    }

    [Serializable]
    public class FloorDataArray
    {
        public FloorData[] images;
    }

    // =======================================================================
    //  VARIABEL PRIVATE
    // =======================================================================

    private FloorDataArray floorDataArray;

    // =======================================================================
    //  PEMBACA JSON & INISIALISASI
    // =======================================================================

    private void Awake()
    {
        data = Analyze.data;

        if (string.IsNullOrEmpty(data))
        {
            Debug.LogError("[Builder] Data JSON kosong! Pastikan Analyze.data sudah terisi sebelum memuat scene ini.");
            return;
        }

        ParseJsonData();

        if (floorDataArray != null && floorDataArray.images != null && floorDataArray.images.Length > 0)
        {
            CreateBuilding();
            var navMesh = FindObjectOfType<PathFinderNavMesh>();
            if (navMesh != null)
            {
                navMesh.BakeNavMesh();
            }
        }
        else
        {
            Debug.LogError("[Builder] Gagal memuat data lantai. Tidak ada struktur bangunan yang dapat dibuat.");
        }
    }

    private void ParseJsonData()
    {
        try
        {
            floorDataArray = JsonUtility.FromJson<FloorDataArray>(data);
            if (floorDataArray == null || floorDataArray.images == null)
            {
                throw new Exception("JSON parsing gagal atau array 'images' tidak ditemukan.");
            }

            Debug.Log($"[Builder] Parsing sukses. Jumlah lantai: {floorDataArray.images.Length}");

            // Hitung rata-rata dimensi pintu dari semua lantai
            float totalAvgDoor = 0f;
            int totalFloors = floorDataArray.images.Length;

            foreach (var floor in floorDataArray.images)
            {
                totalAvgDoor += floor.averageDoor;
            }

            float combinedAvgDoor = totalFloors > 0 ? totalAvgDoor / totalFloors : 0f;

            // Set skala global
            xScale = combinedAvgDoor > 0 ? 1.0f / combinedAvgDoor : 0.01f;
            yScale = xScale;
            originalScale = xScale;

            Debug.Log($"[Builder] Skala Global (1/AvgDoor): {originalScale}");
        }
        catch (Exception e)
        {
            Debug.LogError("[Builder] Error parsing JSON: " + e.Message);
        }
    }

    // =======================================================================
    //  PEMBANGUNAN GEDUNG MULTI-LANTAI
    // =======================================================================

    private void CreateBuilding()
    {
        foreach (var floor in floorDataArray.images)
        {
            float yOffset = floor.floor_index * FLOOR_HEIGHT;
            Debug.Log($"[Builder] Membangun lantai {floor.floor_index} pada Y offset {yOffset}");

            // Buat kontainer untuk setiap lantai agar rapi di Hierarchy
            GameObject floorContainer = new GameObject($"Floor_{floor.floor_index}_Y{yOffset}");

            // Tambahkan dinding lebih dahulu
            for (int i = 0; i < floor.points.Length; i++)
            {
                if (floor.classes[i].name == "wall")
                {
                    CreateObjectForFloor(floor.points[i], floor.classes[i].name, floorContainer.transform, yOffset);
                }
            }

            // Lalu buat pintu dan jendela
            for (int i = 0; i < floor.points.Length; i++)
            {
                string className = floor.classes[i].name;
                if (className != "wall")
                {
                    CreateObjectForFloor(floor.points[i], className, floorContainer.transform, yOffset);
                }
            }
        }
    }

    private void CreateObjectForFloor(Point p, string className, Transform parent, float yOffset)
    {
        GameObject gameObj = new GameObject(className);
        gameObj.transform.SetParent(parent, false);
        AddComponents(gameObj, p, className, yOffset);
    }

    private void AddComponents(GameObject obj, Point p, string className, float yOffset)
    {
        obj.AddComponent<MeshFilter>();
        obj.AddComponent<MeshRenderer>();

        // Gunakan switch expression agar lebih aman di Unity 6 (C# 10+)
        switch (className)
        {
            case "wall":
                obj.tag = "wall";
                var wall = obj.AddComponent<WallMesh>();
                wall.setPoints((float)p.x1, (float)p.y1, (float)p.x2, (float)p.y2, yOffset);
                wall.setGameObjectReference(obj);
                break;

            case "door":
                obj.tag = "door";
                var door = obj.AddComponent<Door>();
                door.setPoints((float)p.x1, (float)p.y1, (float)p.x2, (float)p.y2, yOffset);
                door.setGameObjectReference(obj);
                break;

            case "window":
                obj.tag = "window";
                var window = obj.AddComponent<Window>();
                window.setPoints((float)p.x1, (float)p.y1, (float)p.x2, (float)p.y2, yOffset);
                window.setGameObjectReference(obj);
                break;

            default:
                Debug.LogWarning($"[Builder] Class tidak dikenal: {className}");
                break;
        }
    }
}
