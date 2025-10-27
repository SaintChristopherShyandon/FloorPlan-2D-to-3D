using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Builder : MonoBehaviour
{
    // Tinggi per lantai dalam unit Unity (misalnya 4 meter)
    private const float FLOOR_HEIGHT = 2.5f;

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
            GameObject floorContainer = new GameObject($"Floor_{floor.floor_index}");
            floorContainer.tag = "FloorContainer";
            CreateFloorPlane(floor, floorContainer.transform, yOffset);

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
            if (PathfindingGrid.Instance != null)
            {
                PathfindingGrid.Instance.GenerateGrid();
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

    // =======================================================================
    //  PEMBUATAN LANTAI (FLOOR PLANE)
    // =======================================================================

    private void CreateFloorPlane(FloorData floorData, Transform parent, float yOffset)
    {
        // 1. Cari batas terluar dari semua titik di lantai ini
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minZ = float.MaxValue; // Di Unity, X dari gambar adalah Z
        float maxZ = float.MinValue; // Di Unity, Y dari gambar adalah X

        foreach (var p in floorData.points)
        {
            // Perbarui batas berdasarkan koordinat setiap objek (dinding, pintu, dll.)
            minZ = Mathf.Min(minZ, (float)p.x1, (float)p.x2);
            maxZ = Mathf.Max(maxZ, (float)p.x1, (float)p.x2);
            minX = Mathf.Min(minX, (float)p.y1, (float)p.y2);
            maxX = Mathf.Max(maxX, (float)p.y1, (float)p.y2);
        }

        // Jika tidak ada titik sama sekali, jangan buat lantai
        if (minX == float.MaxValue) return;

        // 2. Terapkan skala global
        minX *= yScale;
        maxX *= yScale;
        minZ *= xScale;
        maxZ *= xScale;

        // 3. Hitung pusat dan ukuran lantai
        float centerX = (minX + maxX) / 2f;
        float centerZ = (minZ + maxZ) / 2f;
        float sizeX = maxX - minX;
        float sizeZ = maxZ - minZ;
        float thickness = 0.1f; // Ketebalan lantai

        // 4. Buat objek lantai (menggunakan Cube tipis)
        GameObject floorPlane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floorPlane.name = "FloorSurface";
        floorPlane.transform.SetParent(parent, false);

        // 5. Atur posisi dan skala
        // Posisi Y diatur agar permukaan atas lantai pas di yOffset
        floorPlane.transform.position = new Vector3(centerX, yOffset - (thickness / 2f), centerZ);
        floorPlane.transform.localScale = new Vector3(sizeX, thickness, sizeZ);

        // Opsional: Beri warna default agar terlihat jelas
        var renderer = floorPlane.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.grey;
        }
    }
}
