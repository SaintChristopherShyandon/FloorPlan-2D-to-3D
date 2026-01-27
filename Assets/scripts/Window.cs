using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Window : MonoBehaviour
{
    public float x1, x2, y1, y2;

    private float floorYOffset;

    GameObject ob;
    public char rotation;

    Bounds meshBounds;

    [Header("Frame / Glass Settings")]
    [SerializeField] private float frameThickness = 0.2f;     // frame thickness
    [SerializeField] private float glassBottomOffset = 0.35f; // glass is higher than bottom

    public void setGameObjectReference(GameObject obj)
    {
        ob = obj;
    }

    void Start()
    {
        GameObject testPrefab = (GameObject)Resources.Load("windowWall");
        Vector3 coordinates = getCoordinates();
        rotation = getRotation();
        Quaternion angle = getAngle(rotation);

        GameObject prefabInstance = Instantiate(testPrefab, coordinates, angle);

        float midX = x1 + (Mathf.Abs(x2 - x1) / 2);
        float midY = y1 + (Mathf.Abs(y2 - y1) / 2);

        WindowGaps windowGapsComponent = prefabInstance.AddComponent<WindowGaps>();
        windowGapsComponent.setParentValues(midX * Builder.xScale, midY * Builder.yScale);
        windowGapsComponent.setOrientation(rotation);
        windowGapsComponent.setValues(x1, y1, x2, y2);
        windowGapsComponent.setRotation(rotation);

        ob.transform.position = coordinates;
        prefabInstance.transform.parent = ob.transform;

        meshBounds = prefabInstance.GetComponent<MeshFilter>().mesh.bounds;
        prefabInstance.transform.localScale = getScale();

        AdjustGlassSize();

        BoxCollider mainBox = prefabInstance.AddComponent<BoxCollider>();
        mainBox.isTrigger = true;

        BoxCollider leftMover = prefabInstance.AddComponent<BoxCollider>();
        BoxCollider rightMover = prefabInstance.AddComponent<BoxCollider>();

        windowGapsComponent.setBoxColliders(mainBox, leftMover, rightMover);

        Rigidbody rb = prefabInstance.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        AddWindowPoints(prefabInstance);
    }

    public Vector3 getCoordinates()
    {
        float xCenter = x1 + (Mathf.Abs(x2 - x1) / 2);
        float yCenter = y1 + (Mathf.Abs(y2 - y1) / 2);
        return new Vector3(yCenter * Builder.yScale, floorYOffset, xCenter * Builder.xScale);
    }

    public Vector3 getScale()
    {
        if (rotation == 'v')
        {
            float yDiff = Mathf.Abs(y1 - y2);
            float scale = yDiff / meshBounds.size.x;
            return new Vector3(scale * Builder.yScale, 1, 1);
        }
        else
        {
            float xDiff = Mathf.Abs(x1 - x2);
            float scale = xDiff / meshBounds.size.x;
            return new Vector3(scale * Builder.xScale, 1, 1);
        }
    }

    public void setPoints(float x1, float y1, float x2, float y2, float yOffset)
    {
        this.x1 = x1;
        this.x2 = x2;
        this.y1 = y1;
        this.y2 = y2;
        this.floorYOffset = yOffset;
    }

    private char getRotation()
    {
        return Mathf.Abs(x1 - x2) > Mathf.Abs(y1 - y2) ? 'h' : 'v';
    }

    private Quaternion getAngle(char c)
    {
        return c == 'h' ? Quaternion.Euler(0, 90, 0) : Quaternion.identity;
    }

    private void AdjustGlassSize()
    {
        if (meshBounds.size == Vector3.zero)
            return;

        float totalHeight = meshBounds.size.y;
        float minFrameHeight = 0.4f; // Minimum combined frame thickness top + bottom

        float maxGlassHeight = totalHeight - (minFrameHeight + frameThickness);

        if (glassBottomOffset > maxGlassHeight)
        {
            glassBottomOffset = Mathf.Max(0f, maxGlassHeight);
        }
    }

    private void AddWindowPoints(GameObject window)
    {
        MeshFilter mf = window.GetComponent<MeshFilter>();
        if (mf == null || mf.mesh == null) return;

        Bounds b = mf.mesh.bounds;

        float spacingX = 0.12f;
        float spacingY = 0.06f;
        float zOffset = 0.02f;

        float outerMinX = b.min.x + frameThickness / 2f - 0.01f;
        float outerMaxX = b.max.x - frameThickness / 2f + 0.01f;
        float outerMinY = b.min.y + frameThickness / 2f - 0.01f;
        float outerMaxY = b.max.y - frameThickness / 2f + 0.01f;

        float innerMinX = b.min.x + frameThickness - 0.02f;
        float innerMaxX = b.max.x - frameThickness + 0.02f;
        float innerMinY = b.min.y + glassBottomOffset - 0.02f;
        float innerMaxY = b.max.y - frameThickness + 0.02f;

        float zFront = b.max.z + zOffset;
        float zBack = b.min.z - zOffset;

        for (float x = outerMinX; x <= outerMaxX; x += spacingX)
        {
            for (float y = outerMinY; y <= outerMaxY; y += spacingY)
            {
                bool inFrameVertically = y >= outerMinY && y <= outerMaxY;
                bool inFrameHorizontally = x >= outerMinX && x <= outerMaxX;

                // Fill the ring-shaped frame area around the glass:
                bool inRing = ((x <= innerMinX || x >= innerMaxX) && inFrameVertically) ||
                              ((y <= innerMinY || y >= innerMaxY) && inFrameHorizontally);

                if (inRing)
                {
                    SpawnPoint(window.transform, new Vector3(x, y, zFront));
                    SpawnPoint(window.transform, new Vector3(x, y, zBack));
                }
            }
        }
    }

    private void SpawnPoint(Transform parent, Vector3 localPos)
    {
        GameObject go = new GameObject("point");
        go.layer = LayerMask.NameToLayer("Point");
        go.tag = "point";

        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = Vector3.one * 0.05f;

        SphereCollider col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.5f;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        go.AddComponent<PointNode>();
    }
}