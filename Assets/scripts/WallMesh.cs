using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallMesh : MonoBehaviour
{
    Vector3[] meshDataVertices;
    public float x1, y1, x2, y2;
    private float floorYOffset = 0f;

    GameObject ob;
    public char rotation;
    public bool isFiller = false;
    Bounds meshBounds;
    Vector3 scale;

    // Variabel statis untuk akumulasi ketebalan tembok
    public static float totalThickness = 0f;
    public static int wallCount = 0;

    public void setGameObjectReference(GameObject obj)
    {
        ob = obj;
    }

    public void setPoints(float x1, float y1, float x2, float y2, float yOffset)
    {
        this.x1 = x1;
        this.x2 = x2;
        this.y1 = y1;
        this.y2 = y2;
        this.floorYOffset = yOffset;
    }

    public void setPoints(float x1, float y1, float x2, float y2)
    {
        this.x1 = x1;
        this.x2 = x2;
        this.y1 = y1;
        this.y2 = y2;
        this.floorYOffset = 0f;
    }

    void Start()
    {
        // Buat objek tembok dasar
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.layer = 9;
        cube.name = "wall";
        cube.transform.position = Vector3.zero;

        Vector3 coordinates = getCoordinates();
        rotation = getRotation();
        Quaternion angle = getAngle(rotation);

        meshBounds = cube.GetComponent<MeshFilter>().mesh.bounds;
        scale = getScale();
        cube.transform.localScale = scale;
        cube.transform.rotation = angle;
        cube.transform.parent = gameObject.transform;
        transform.position = coordinates;

        // collider utama (trigger)
        BoxCollider mainCollider = cube.GetComponent<BoxCollider>();
        mainCollider.isTrigger = true;

        cube.AddComponent<Rigidbody>().isKinematic = true;

        // Spawn titik seperti di Builder.cs
        AddWallPoints(cube);
    }

    private void AddWallPoints(GameObject wall)
    {
        Vector3 scale = wall.transform.localScale;
        Vector3 center = wall.transform.position;
        Quaternion rot = wall.transform.rotation;

        float spacing = 0.15f;
        // Gunakan Math.Max agar minimal ada 1 baris titik walaupun tembok kecil
        int numX = Mathf.Max(1, Mathf.FloorToInt(scale.x / spacing));
        int numY = Mathf.Max(1, Mathf.FloorToInt(scale.y / spacing));
        
        float halfZ = scale.z / 2f;
        float offsetOut = 0.02f; // Sedikit keluar dari tembok agar tidak tenggelam

        // Pola: Depan dan Belakang tembok
        float[] sides = { halfZ + offsetOut, -halfZ - offsetOut };

        foreach (float sideZ in sides)
        {
            for (int i = 0; i <= numX; i++)
            {
                for (int j = 0; j <= numY; j++)
                {
                    float offsetX = -scale.x / 2 + i * spacing;
                    float offsetY = -scale.y / 2 + j * spacing;

                    Vector3 localPos = new Vector3(offsetX, offsetY, sideZ);
                    Vector3 worldPos = rot * localPos + center;

                    GameObject go = new GameObject("point");
                    go.layer = LayerMask.NameToLayer("Point");
                    // 1. Set Parent dulu
                    go.transform.SetParent(wall.transform, false); 
                    go.transform.position = worldPos;

                    // 2. PERBAIKAN BENTUK (Agar tidak gepeng)
                    // Kita ambil skala asli (LossyScale) dari Wall
                    Vector3 parentGlobalScale = wall.transform.lossyScale;
                    float desiredSize = 0.05f; // Ukuran bulat yang kamu mau

                    // Rumus: Ukuran Target / Ukuran Parent
                    // Ini akan memaksa point tetap bulat 0.05f walau temboknya gepeng
                    go.transform.localScale = new Vector3(
                        desiredSize / parentGlobalScale.x,
                        desiredSize / parentGlobalScale.y,
                        desiredSize / parentGlobalScale.z
                    );

                    go.tag = "point";

                    // 3. Collider
                    SphereCollider col = go.AddComponent<SphereCollider>();
                    col.isTrigger = true;
                    
                    // Samakan radius ini dengan yang ada di Floor/Roof (disana kamu pakai 0.5f)
                    // Tapi ingat, radius ini relatif terhadap localScale point. 
                    // Jika visual point kecil, radius 0.5f mungkin cukup besar (jangkauan luas).
                    col.radius = 0.5f; 

                    Rigidbody rb = go.AddComponent<Rigidbody>();
                    rb.isKinematic = true;

                    go.AddComponent<PointNode>();
                }
            }
        }
    }

    private Quaternion getAngle(char c)
    {
        switch (c)
        {
            case 'v': return Quaternion.identity; // menghadap ke Z
            case 'h': return Quaternion.Euler(0, 90, 0); // menghadap ke X
            default: return Quaternion.identity;
        }
    }

    private char getRotation()
    {
        float xDiff = Mathf.Abs(x1 - x2);
        float yDiff = Mathf.Abs(y1 - y2);
        if (xDiff > yDiff) return 'h';
        if (yDiff > xDiff) return 'v';
        return 'n';
    }

    public Vector3 getScale()
    {
        float yDiff = Mathf.Abs(y1 - y2);
        float xDiff = Mathf.Abs(x1 - x2);
        float meshBoundY = meshBounds.size.x;
        float meshBoundX = meshBounds.size.z;
        float scaleY = yDiff / meshBoundY;
        float scaleX = xDiff / meshBoundX;

        Vector3 finalScale;
        float thickness;
        if (rotation == 'v')
        {
            finalScale = new Vector3(scaleY * Builder.yScale, 2.5f, scaleX * Builder.xScale);
            thickness = scaleX * Builder.xScale;
        }
        else
        {
            finalScale = new Vector3(scaleX * Builder.xScale, 2.5f, scaleY * Builder.yScale);
            thickness = scaleY * Builder.yScale;
        }

        // Akumulasikan ketebalan untuk rata-rata
        totalThickness += thickness;
        wallCount++;

        return finalScale;
    }

    public Vector3 getCoordinates()
    {
        float xCenter = x1 + (Mathf.Abs(x2 - x1) / 2);
        float yCenter = y1 + (Mathf.Abs(y2 - y1) / 2);
        return new Vector3(yCenter * Builder.yScale, 1.25f + floorYOffset, xCenter * Builder.xScale);
    }
}