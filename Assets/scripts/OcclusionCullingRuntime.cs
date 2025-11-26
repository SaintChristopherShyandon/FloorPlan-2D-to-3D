// OcclusionCullingRuntime.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class OcclusionCullingRuntime : MonoBehaviour
{
    [Header("General")]
    public Camera targetCamera; // jika kosong, akan otomatis cari Camera pada GameObject ini
    [Tooltip("Layer(s) that act as occluders (e.g. walls, large meshes).")]
    public LayerMask occluderLayer;
    [Tooltip("Which layers contain objects to test for occlusion.")]
    public LayerMask occludeeLayer;
    [Tooltip("Max distance to consider for occlusion checks. Objects farther than this will be ignored.")]
    public float maxCheckDistance = 200f;
    [Tooltip("Time (sec) between checks for each object (staggered).")]
    public float perObjectCheckInterval = 0.25f;
    [Tooltip("If true, will disable Renderer.enabled. If false, will SetActive(false) on whole GameObject.")]
    public bool disableRendererOnly = true;

    [Header("Sampling & Accuracy")]
    [Tooltip("Number of sample points per object's bounds to raycast. 1 = center only; more increases accuracy.")]
    [Range(1, 9)]
    public int samplesPerObject = 3; // e.g., center + 2 random points
    [Tooltip("If true, uses multiple frames to stagger tests (better for many objects).")]
    public bool staggerChecks = true;

    [Header("Debug")]
    public bool debugDrawRays = false;
    public bool debugLog = false;

    // internal
    private Plane[] frustumPlanes;
    private List<Renderer> occludees = new List<Renderer>();
    private Dictionary<Renderer, float> nextCheckTime = new Dictionary<Renderer, float>();
    private System.Random rnd = new System.Random();

    void Awake()
    {
        if (targetCamera == null) targetCamera = GetComponent<Camera>() ?? Camera.main;
        if (targetCamera == null)
        {
            Debug.LogError("[OcclusionCullingRuntime] No camera found. Attach to camera or assign targetCamera.");
            enabled = false;
            return;
        }
    }

    void Start()
    {
        // Find initial occludees in scene by layer mask
        RefreshOccludees();
        StartCoroutine(PeriodicRefreshOccludees());
    }

    IEnumerator PeriodicRefreshOccludees()
    {
        // occasionally refresh list (in case of dynamic spawn/despawn)
        var wait = new WaitForSeconds(5f);
        while (true)
        {
            yield return wait;
            RefreshOccludees();
        }
    }

    /// <summary>
    /// Finds all renderers on occludeeLayer and adds them to list (excluding the ones on occluderLayer).
    /// </summary>
    public void RefreshOccludees()
    {
        occludees.Clear();
        nextCheckTime.Clear();
        // Find all renderers in loaded scene — filter by layer mask.
        Renderer[] all = GameObject.FindObjectsOfType<Renderer>();
        foreach (var r in all)
        {
            int objLayerMask = 1 << r.gameObject.layer;
            if ((occludeeLayer.value & objLayerMask) != 0)
            {
                // optionally skip if object is occluder (same time)
                occludees.Add(r);
                nextCheckTime[r] = Time.time + (float)rnd.NextDouble() * perObjectCheckInterval; // stagger start
            }
        }

        if (debugLog) Debug.Log($"[OcclusionCullingRuntime] Found {occludees.Count} occludee renderers.");
    }

    void Update()
    {
        // compute frustum planes once per frame
        frustumPlanes = GeometryUtility.CalculateFrustumPlanes(targetCamera);

        // iterate occludees and check (staggered)
        for (int i = 0; i < occludees.Count; i++)
        {
            Renderer rend = occludees[i];
            if (rend == null) continue;

            // skip if object disabled or invisible by layer or too far
            if (!rend.gameObject.activeInHierarchy)
            {
                // ensure renderer is disabled as well (consistency)
                if (disableRendererOnly && rend.enabled) rend.enabled = false;
                continue;
            }

            if (staggerChecks)
            {
                if (Time.time < nextCheckTime[rend]) continue;
                nextCheckTime[rend] = Time.time + perObjectCheckInterval;
            }

            CheckAndApplyOcclusion(rend);
        }
    }

    void CheckAndApplyOcclusion(Renderer rend)
    {
        Bounds b = rend.bounds;

        // quick distance cull
        float dist = Vector3.Distance(targetCamera.transform.position, b.center);
        if (dist > maxCheckDistance)
        {
            // too far: leave enabled (or you may want to disable)
            SetRendererActive(rend, true);
            return;
        }

        // frustum test
        if (!GeometryUtility.TestPlanesAABB(frustumPlanes, b))
        {
            // outside camera frustum -> safe to disable
            SetRendererActive(rend, false);
            return;
        }

        // inside frustum -> do occlusion ray tests
        int visibleSamples = 0;
        int samples = Mathf.Max(1, samplesPerObject);

        // prepare sample points: center + random points on bounds surface
        List<Vector3> points = new List<Vector3>(samples);
        points.Add(b.center);
        for (int s = 1; s < samples; s++)
        {
            // random point inside bounds
            float rx = (float)(rnd.NextDouble() * 2.0 - 1.0);
            float ry = (float)(rnd.NextDouble() * 2.0 - 1.0);
            float rz = (float)(rnd.NextDouble() * 2.0 - 1.0);
            Vector3 local = new Vector3(rx * b.extents.x, ry * b.extents.y, rz * b.extents.z);
            points.Add(b.center + local);
        }

        Vector3 camPos = targetCamera.transform.position;
        foreach (var p in points)
        {
            Vector3 dir = p - camPos;
            float distanceToPoint = dir.magnitude;
            dir.Normalize();

            RaycastHit hit;
            bool h = Physics.Raycast(camPos, dir, out hit, distanceToPoint, occluderLayer.value);
            if (debugDrawRays)
            {
                Color col = h ? Color.red : Color.green;
                Debug.DrawRay(camPos, dir * distanceToPoint, col, perObjectCheckInterval * 0.9f);
            }

            if (!h)
            {
                // no occluder between camera and point -> visible
                visibleSamples++;
                // small optimization: if one sample visible, break (tolerate partial occlusion)
                if (visibleSamples > 0) break;
            }
        }

        bool isVisible = visibleSamples > 0;
        SetRendererActive(rend, isVisible);
    }

    void SetRendererActive(Renderer rend, bool active)
    {
        if (disableRendererOnly)
        {
            if (rend.enabled != active)
            {
                rend.enabled = active;
                if (debugLog) Debug.Log($"[OcclusionCullingRuntime] Renderer '{rend.gameObject.name}' set enabled={active}");
            }
        }
        else
        {
            if (rend.gameObject.activeSelf != active)
            {
                rend.gameObject.SetActive(active);
                if (debugLog) Debug.Log($"[OcclusionCullingRuntime] GameObject '{rend.gameObject.name}' set active={active}");
            }
        }
    }

    // For usability in editor and runtime
    private void OnValidate()
    {
        if (samplesPerObject < 1) samplesPerObject = 1;
        if (perObjectCheckInterval < 0.01f) perObjectCheckInterval = 0.01f;
    }

    // Optional gizmos to show occludee bounds (only in editor)
    private void OnDrawGizmosSelected()
    {
        if (!debugDrawRays) return;
        if (occludees == null) return;
        Gizmos.color = Color.yellow;
        foreach (var r in occludees)
        {
            if (r == null) continue;
            Gizmos.DrawWireCube(r.bounds.center, r.bounds.size);
        }
    }
}
