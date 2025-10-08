using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class Builder : MonoBehaviour
{
    // Konstanta Skala dan Tinggi Lantai
    // Ketinggian dalam unit Unity untuk setiap lantai (misalnya 4 meter)
    private const float FLOOR_HEIGHT = 4.0f; 
    
    // Data statis
    public static float xScale;
    public static float yScale;
    public static float originalScale;
    public static string data;
    
    // Variabel Instansiasi
    public GameObject spwaner; // (Tidak digunakan dalam kode yang dimodifikasi, tetapi dipertahankan)

    // =========================================================================
    // JSON DESERIALIZATION CLASSES (BARU UNTUK MULTI-LANTAI)
    // =========================================================================
    
    [System.Serializable]
    public class Point
    {
        public double x1; // xmin
        public double y1; // ymin
        public double x2; // xmax
        public double y2; // ymax
    }
    
    [System.Serializable]
    public class NamesSub
    {
        public string name; // wall, window, or door
    }

    [System.Serializable]
    // Kelas untuk data satu lantai (sama dengan struktur respons API tunggal)
    public class FloorData
    {
        public int floor_index;
        public Point[] points;
        public NamesSub[] classes;
        public int Width;
        public int Height;
        public float averageDoor;
    }
    
    [System.Serializable]
    // Kelas Pembungkus Root (Sesuai dengan {"images": [...]})
    public class FloorDataArray
    {
        public FloorData[] images;
    }

    // =========================================================================
    // READER DAN AWAKE
    // =========================================================================
    
    private FloorDataArray floorDataArray;

    private void localReaderData()
    {
        if (string.IsNullOrEmpty(data))
        {
            Debug.LogError("Data JSON kosong. Unggah gambar terlebih dahulu.");
            return;
        }

        try
        {
            // Deserialize JSON ke struktur array multi-lantai
            floorDataArray = JsonUtility.FromJson<FloorDataArray>(data);
            if (floorDataArray == null || floorDataArray.images == null)
            {
                throw new System.Exception("JSON parsing failed or 'images' array is missing.");
            }
            Debug.Log($"Successfully parsed {floorDataArray.images.Length} floors from JSON.");
            
            // Tentukan skala global berdasarkan rata-rata semua pintu
            float totalAvgDoor = 0f;
            int totalFloors = floorDataArray.images.Length;

            foreach(var floor in floorDataArray.images)
            {
                totalAvgDoor += floor.averageDoor;
            }

            float combinedAvgDoor = (totalFloors > 0) ? totalAvgDoor / totalFloors : 0f;
            
            // Set skala global
            xScale = (combinedAvgDoor > 0) ? 1.0f / combinedAvgDoor : 0.01f;
            yScale = xScale; // Skala X dan Y harus sama
            originalScale = xScale;
            
            Debug.Log($"Skala Global (1/AvgDoor): {originalScale}");


        }
        catch (System.Exception e)
        {
            Debug.LogError("Error parsing JSON data: " + e.Message);
        }
    }

    private void Awake()
    {
        data = Analyze.data;
        localReaderData();
        
        if (floorDataArray != null && floorDataArray.images != null)
        {
            createBuilding();
        }
        else
        {
            Debug.LogError("Gagal memuat data lantai. Tidak dapat membangun gedung.");
        }

        // Pastikan NavMesh dibake setelah semua objek dibuat
        FindObjectOfType<PathFinderNavMesh>()?.BakeNavMesh();
    }
    
    // =========================================================================
    // PEMBANGUNAN GEDUNG MULTI-LANTAI
    // =========================================================================

    private void createBuilding()
    {
        // Iterasi melalui setiap data lantai yang diterima dari API
        foreach (FloorData floor in floorDataArray.images)
        {
            // Hitung offset vertikal untuk lantai ini
            // Floor 0: 0 * HEIGHT
            // Floor 1: 1 * HEIGHT
            // Floor 2: 2 * HEIGHT, dst.
            float yOffset = floor.floor_index * FLOOR_HEIGHT; 
            
            Debug.Log($"Mulai membangun Lantai ke-{floor.floor_index} dengan offset Y: {yOffset}");

            // 1. Buat kontainer untuk lantai agar mudah diatur di Hierarchy
            GameObject floorContainer = new GameObject($"Floor_{floor.floor_index}_Y{yOffset}");
            
            // 2. Buat Dinding terlebih dahulu
            for (int i = 0; i < floor.points.Length; i++)
            {
                if (floor.classes[i].name == "wall")
                {
                    createObjectForFloor(floor.points[i], floor.classes[i].name, floorContainer.transform, yOffset);
                }
            }
            
            // 3. Buat Pintu dan Jendela
            for (int i = 0; i < floor.points.Length; i++)
            {
                string className = floor.classes[i].name;
                if (className != "wall")
                {
                    createObjectForFloor(floor.points[i], className, floorContainer.transform, yOffset);
                }
            }
        }
    }

    private void createObjectForFloor(Point p, string className, Transform parent, float yOffset)
    {
        GameObject gameObj = new GameObject(className);
        gameObj.transform.SetParent(parent); // Set parent ke kontainer lantai
        
        // Panggil komponen adder dengan offset Y
        componentsAdder(gameObj, p, className, yOffset);
    }
    
    void componentsAdder(GameObject obj, Point p, string className, float yOffset)
    {
        obj.AddComponent<MeshFilter>();
        obj.AddComponent<MeshRenderer>();
        
        // Offset Y diaplikasikan di script WallMesh, Door, dan Window Anda
        // Kita perlu memodifikasi script WallMesh/Door/Window agar dapat menerima yOffset
        // ASUMSI: Script WallMesh, Door, dan Window memiliki fungsi setPoints baru:
        // setPoints(float x1, float y1, float x2, float y2, float yOffset)
        
        if (className.Equals("wall"))
        {
            obj.tag = "wall";
            obj.AddComponent<WallMesh>();
            WallMesh temp = obj.GetComponent<WallMesh>();
            // PENTING: WallMesh harus diubah untuk menerima yOffset dan menggunakannya!
            // Kita panggil fungsi dengan 5 parameter
            temp.setPoints((float)p.x1, (float)p.y1, (float)p.x2, (float)p.y2, yOffset); 
            temp.setGameObjectReference(obj);
        }
        else if (className.Equals("door"))
        {
            obj.tag = "door";
            obj.AddComponent<Door>();
            Door temp = obj.GetComponent<Door>();
            // PENTING: Door harus diubah untuk menerima yOffset
            temp.setPoints((float)p.x1, (float)p.y1, (float)p.x2, (float)p.y2, yOffset);
            temp.setGameObjectReference(obj);
        }
        else if (className.Equals("window"))
        {
            obj.tag = "window";
            obj.AddComponent<Window>();
            Window temp = obj.GetComponent<Window>();
            // PENTING: Window harus diubah untuk menerima yOffset
            temp.setPoints((float)p.x1, (float)p.y1, (float)p.x2, (float)p.y2, yOffset);
            temp.setGameObjectReference(obj);
        }
    }
    
    // Fungsi Start() lama (FindObjectOfType<PathFinderNavMesh>()?.FindUsingMarkers();) dihapus karena BakeNavMesh() dilakukan di Awake().
}
