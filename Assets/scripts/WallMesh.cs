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
    int wallPaperIds = 0;
    Vector3 scale;

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

        // spawn titik kecil di permukaan
        addWallPaperPoints(cube);
    }

    private void wallPaperPoints(Vector3 pos, string name)
    {
        GameObject go = new GameObject(name);
        go.tag = "wallPaperr";
        go.transform.position = pos;
        go.transform.parent = transform;

        // collider kecil biar bisa diklik nanti
        BoxCollider goCollider = go.AddComponent<BoxCollider>();
        goCollider.isTrigger = true;
        goCollider.size = new Vector3(0.1f, 0.1f, 0.1f);

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        WallPaper wp = go.AddComponent<WallPaper>();
        wp.addId(this.wallPaperIds);
        this.wallPaperIds++;
    }

    public void addWallPaperPoints(GameObject cube)
    {
        Vector3 wallScale = cube.transform.localScale;
        Vector3 wallCenter = cube.transform.position;
        Quaternion wallRotation = cube.transform.rotation;

        float spacing = 0.25f;
        // ***** PERUBAHAN INTI DI SINI *****
        // gunakan lebar selalu dari local X (local space) — kemudian tempatkan titik pada local Z = +halfZ
        float width = wallScale.x;   // gunakan local X sebagai lebar grid (nanti TransformPoint meng-handle rotasi)
        float height = wallScale.y;

        int numX = Mathf.CeilToInt(width / spacing);
        int numY = Mathf.CeilToInt(height / spacing);

        // local half depth (permukaan depan di local +Z)
        float localHalfZ = wallScale.z / 2f;

        for (int i = 0; i <= numX; i++)
        {
            for (int j = 0; j <= numY; j++)
            {
                float offsetX = -width / 2 + i * spacing;
                float offsetY = -height / 2 + j * spacing;

                // Buat posisi di local space cube: (localX, localY, localZ)
                // localZ kita pakai localHalfZ agar titik menempel ke permukaan depan local (+Z).
                Vector3 localPos = new Vector3(offsetX, offsetY, localHalfZ);

                // Convert ke world pos (mengikuti cube.transform.rotation)
                Vector3 worldPos = wallRotation * localPos + wallCenter;

                wallPaperPoints(worldPos, "wallpaper_point");
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

        if (rotation == 'v')
            return new Vector3(scaleY * Builder.yScale, 2.5f, scaleX * Builder.xScale);
        else
            return new Vector3(scaleX * Builder.xScale, 2.5f, scaleY * Builder.yScale);
    }

    public Vector3 getCoordinates()
    {
        float xCenter = x1 + (Mathf.Abs(x2 - x1) / 2);
        float yCenter = y1 + (Mathf.Abs(y2 - y1) / 2);
        return new Vector3(yCenter * Builder.yScale, 1.25f + floorYOffset, xCenter * Builder.xScale);
    }
}
