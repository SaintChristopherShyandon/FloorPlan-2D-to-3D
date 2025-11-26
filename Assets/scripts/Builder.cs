using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Builder : MonoBehaviour
{
    private const float FLOOR_HEIGHT = 2.5f;

    public static float xScale;
    public static float yScale;
    public static float originalScale;
    public static string data;

    [Header("Optional Spawner Reference")]
    public GameObject spwaner;

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

    private FloorDataArray floorDataArray;

    private void Awake()
    {
        data = Analyze.data;

        if (string.IsNullOrEmpty(data))
        {
            Debug.LogError("[Builder] Data JSON kosong!");
            return;
        }

        ParseJsonData();

        if (floorDataArray != null && floorDataArray.images != null && floorDataArray.images.Length > 0)
        {
            CreateBuilding();
            var navMesh = FindObjectOfType<PathFinderNavMesh>();
            if (navMesh != null)
                navMesh.BakeNavMesh();
        }
        else
        {
            Debug.LogError("[Builder] Gagal memuat data lantai.");
        }
    }

    private void ParseJsonData()
    {
        try
        {
            floorDataArray = JsonUtility.FromJson<FloorDataArray>(data);
            if (floorDataArray == null || floorDataArray.images == null)
                throw new Exception("JSON parsing gagal atau array 'images' tidak ditemukan.");

            float totalAvgDoor = 0f;
            int totalFloors = floorDataArray.images.Length;

            foreach (var floor in floorDataArray.images)
                totalAvgDoor += floor.averageDoor;

            float combinedAvgDoor = totalFloors > 0 ? totalAvgDoor / totalFloors : 0f;

            xScale = combinedAvgDoor > 0 ? 1.0f / combinedAvgDoor : 0.01f;
            yScale = xScale;
            originalScale = xScale;

            Debug.Log($"[Builder] Skala Global: {originalScale}");
        }
        catch (Exception e)
        {
            Debug.LogError("[Builder] Error parsing JSON: " + e.Message);
        }
    }

    private void CreateBuilding()
    {
        int totalFloors = floorDataArray.images.Length;

        for (int i = 0; i < totalFloors; i++)
        {
            var floor = floorDataArray.images[i];
            float yOffset = floor.floor_index * FLOOR_HEIGHT;

            GameObject floorContainer = new GameObject($"Floor_{floor.floor_index}");
            floorContainer.tag = "FloorContainer";

            CreateFloorPlane(floor, floorContainer.transform, yOffset);

            // Buat objek bangunan
            for (int j = 0; j < floor.points.Length; j++)
            {
                string className = floor.classes[j].name;
                CreateObjectForFloor(floor.points[j], className, floorContainer.transform, yOffset);
            }

            // Tambahkan atap di lantai terakhir
            if (i == totalFloors - 1)
            {
                float roofHeight = yOffset + FLOOR_HEIGHT;
                CreateRoofPlane(floor, floorContainer.transform, roofHeight);
            }

            if (PathfindingGrid.Instance != null)
                PathfindingGrid.Instance.GenerateGrid();
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
        }
    }

    // ============================ FLOOR ============================

    private void CreateFloorPlane(FloorData floorData, Transform parent, float yOffset)
    {
        float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (var p in floorData.points)
        {
            minZ = Mathf.Min(minZ, (float)p.x1, (float)p.x2);
            maxZ = Mathf.Max(maxZ, (float)p.x1, (float)p.x2);
            minX = Mathf.Min(minX, (float)p.y1, (float)p.y2);
            maxX = Mathf.Max(maxX, (float)p.y1, (float)p.y2);
        }

        if (minX == float.MaxValue) return;

        minX *= yScale; maxX *= yScale;
        minZ *= xScale; maxZ *= xScale;

        float centerX = (minX + maxX) / 2f;
        float centerZ = (minZ + maxZ) / 2f;
        float sizeX = maxX - minX;
        float sizeZ = maxZ - minZ;
        float thickness = 0.1f;

        GameObject floorPlane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floorPlane.name = "FloorSurface";
        floorPlane.transform.SetParent(parent, false);
        floorPlane.transform.position = new Vector3(centerX, yOffset - thickness / 2f, centerZ);
        floorPlane.transform.localScale = new Vector3(sizeX, thickness, sizeZ);
        floorPlane.GetComponent<MeshRenderer>().material.color = Color.grey;
        floorPlane.AddComponent<Rigidbody>().isKinematic = true;

        AddInvisiblePoints(floorPlane);
    }

    // ============================ ROOF ============================

    private void CreateRoofPlane(FloorData floorData, Transform parent, float yOffset)
    {
        float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (var p in floorData.points)
        {
            minZ = Mathf.Min(minZ, (float)p.x1, (float)p.x2);
            maxZ = Mathf.Max(maxZ, (float)p.x1, (float)p.x2);
            minX = Mathf.Min(minX, (float)p.y1, (float)p.y2);
            maxX = Mathf.Max(maxX, (float)p.y1, (float)p.y2);
        }

        if (minX == float.MaxValue) return;

        minX *= yScale; maxX *= yScale;
        minZ *= xScale; maxZ *= xScale;

        float centerX = (minX + maxX) / 2f;
        float centerZ = (minZ + maxZ) / 2f;
        float sizeX = maxX - minX;
        float sizeZ = maxZ - minZ;
        float thickness = 0.1f;

        GameObject roofPlane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roofPlane.name = "RoofSurface";
        roofPlane.transform.SetParent(parent, false);
        roofPlane.transform.position = new Vector3(centerX, yOffset + thickness / 2f, centerZ);
        roofPlane.transform.localScale = new Vector3(sizeX, thickness, sizeZ);
        roofPlane.GetComponent<MeshRenderer>().material.color = new Color(0.5f, 0, 0);
        roofPlane.AddComponent<Rigidbody>().isKinematic = true;

        AddInvisiblePoints(roofPlane);
    }

    // ============================ INVISIBLE POINTS ============================

    private void AddInvisiblePoints(GameObject plane)
    {
        Vector3 scale = plane.transform.localScale;
        Vector3 center = plane.transform.position;
        Quaternion rot = plane.transform.rotation;

        float spacing = 0.15f;
        int numX = Mathf.Max(1, Mathf.FloorToInt(scale.x / spacing));
        int numZ = Mathf.Max(1, Mathf.FloorToInt(scale.z / spacing));
        float localHalfY = scale.y / 2f;
        float[] sides = { localHalfY, -localHalfY };

        foreach (float sideY in sides)
        {
            for (int i = 0; i <= numX; i++)
            {
                for (int j = 0; j <= numZ; j++)
                {
                    float offsetX = -scale.x / 2 + i * spacing;
                    float offsetZ = -scale.z / 2 + j * spacing;
                    Vector3 localPos = new Vector3(offsetX, sideY, offsetZ);
                    Vector3 worldPos = rot * localPos + center;

                    // === BUAT POINT TANPA RENDERER ===
                    GameObject go = new GameObject("point");
                    go.transform.position = worldPos;
                    go.transform.localScale = Vector3.one * 0.05f;
                    go.transform.parent = plane.transform;
                    go.tag = "point";
                    go.layer = LayerMask.NameToLayer("Point");
                    SphereCollider col = go.AddComponent<SphereCollider>();
                    col.isTrigger = true;
                    col.radius = 0.5f;

                    Rigidbody rb = go.AddComponent<Rigidbody>();
                    rb.isKinematic = true;

                    go.AddComponent<PointNode>();
                }
            }
        }
    }
}
