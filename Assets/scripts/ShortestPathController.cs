using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering; // untuk BlendMode, RenderQueue

public class ShortestPathController : MonoBehaviour
{
    [Header("Pathfinding")]
    public GraphManager graphManager;
    private PointNode startNode = null;
    private List<PointNode> destinationNodes = new List<PointNode>();

    [Header("Transparency Controls")]
    [Tooltip("Tags yang dianggap target transparansi")]
    public string[] targetTags = new[] { "Wall", "Roof", "Floor" };

    [Tooltip("Juga deteksi nama objek yang mengandung 'wall/roof/floor' (jika tidak pakai Tag)")]
    public bool useNameFallback = true;

    [Range(0f, 1f)]
    [Tooltip("65% transparan = 0.35 alpha")]
    public float targetAlpha = 0.35f;

    // Cache material untuk toggle/restore
    private readonly Dictionary<Renderer, Material[]> originalSharedMats = new Dictionary<Renderer, Material[]>();
    private readonly Dictionary<Renderer, Material[]> transparentInstancedMats = new Dictionary<Renderer, Material[]>();

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main != null
                ? Camera.main.ScreenPointToRay(Input.mousePosition)
                : new Ray(Vector3.zero, Vector3.forward);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                PointNode node = hit.collider.GetComponent<PointNode>();
                if (node != null)
                    HandleClick(node);
            }
        }
    }

    void HandleClick(PointNode node)
    {
        if (startNode == null)
        {
            startNode = node;
            node.SetAsStart();
        }
        else if (!destinationNodes.Contains(node) && node != startNode)
        {
            destinationNodes.Add(node);
            node.SetAsDestination();
        }
    }

    public void OnClickFindPath()
    {
        if (startNode == null || destinationNodes.Count == 0)
        {
            Debug.LogWarning("Start atau tujuan belum dipilih!");
            return;
        }

        graphManager.BuildConnections(); // rebuild agar koneksi selalu up-to-date
        graphManager.FindShortestPath(startNode, destinationNodes);
    }

    public void OnClickReset()
    {
        graphManager.ClearAllPaths();

        foreach (var node in FindObjectsOfType<PointNode>())
            node.ResetSelection();

        startNode = null;
        destinationNodes.Clear();

        // Kembalikan semua material yang dibuat transparan
        RestoreAllTransparency();
    }

    // -------------------------------------------------
    // BUTTON: Jadikan semua Wall/Roof/Floor transparan 65%
    // Hubungkan method ini ke UI Button OnClick
    // -------------------------------------------------
    public void OnClickMakeAllTransparent()
    {
        MakeAllTargetsTransparent(targetAlpha);
        Debug.Log($"Semua objek bertag/nama Wall/Roof/Floor diset transparan (alpha={targetAlpha:0.00}).");
    }

    private void MakeAllTargetsTransparent(float alpha)
    {
        // Ambil semua renderer di scene (aktif/non-aktif) lalu filter target
        var allRenderers = FindObjectsOfType<Renderer>(true);
        int count = 0;

        for (int i = 0; i < allRenderers.Length; i++)
        {
            var r = allRenderers[i];
            if (r == null) continue;

            // Periksa tag pada object atau parent; atau fallback per nama
            if (IsTransparencyTarget(r.transform))
            {
                MakeRendererTransparent(r, alpha);
                count++;
            }
        }

        if (count == 0)
            Debug.LogWarning("Tidak ditemukan renderer bertag/nama Wall/Roof/Floor. Pastikan Tag atau nama objek sesuai.");
    }

    private bool IsTransparencyTarget(Transform t)
    {
        // Cek tag pada transform dan semua parent
        var cur = t;
        while (cur != null)
        {
            for (int i = 0; i < targetTags.Length; i++)
            {
                if (cur.CompareTag(targetTags[i])) return true;
            }
            cur = cur.parent;
        }

        if (!useNameFallback) return false;

        // Fallback: cek nama transform dan parent (case-insensitive)
        cur = t;
        while (cur != null)
        {
            string name = cur.name.ToLowerInvariant();
            if (name.Contains("wall") || name.Contains("roof") || name.Contains("floor"))
                return true;
            cur = cur.parent;
        }

        return false;
    }

    // --------------------------
    // Material helpers
    // --------------------------
    private void MakeRendererTransparent(Renderer r, float alpha)
    {
        if (r == null) return;

        // Simpan shared materials asli (sekali saja)
        if (!originalSharedMats.ContainsKey(r))
        {
            originalSharedMats[r] = r.sharedMaterials; // referensi, tidak instansiasi
        }

        // Jika sudah pernah dibuat instanced transparent, pakai cache
        if (transparentInstancedMats.TryGetValue(r, out var cached))
        {
            r.materials = cached;
            return;
        }

        // Buat instanced materials dari shared, lalu set ke transparent
        var shared = r.sharedMaterials;
        var instanced = new Material[shared.Length];

        for (int i = 0; i < shared.Length; i++)
        {
            var baseMat = shared[i];
            if (baseMat == null)
            {
                instanced[i] = null;
                continue;
            }

            var m = new Material(baseMat);
            SetMaterialTransparent(m, alpha);
            instanced[i] = m;
        }

        r.materials = instanced;
        transparentInstancedMats[r] = instanced;
    }

    private void RestoreRenderer(Renderer r)
    {
        if (r == null) return;

        if (originalSharedMats.TryGetValue(r, out var original))
        {
            r.sharedMaterials = original;
        }

        if (transparentInstancedMats.TryGetValue(r, out var instanced))
        {
            for (int i = 0; i < instanced.Length; i++)
            {
                if (instanced[i] != null)
                    Destroy(instanced[i]);
            }
            transparentInstancedMats.Remove(r);
        }
    }

    private void RestoreAllTransparency()
    {
        var toRestore = new List<Renderer>(originalSharedMats.Keys);
        for (int i = 0; i < toRestore.Count; i++)
            RestoreRenderer(toRestore[i]);

        originalSharedMats.Clear();
        transparentInstancedMats.Clear();
    }

    // Standard/URP transparent setup
    private void SetMaterialTransparent(Material mat, float alpha)
    {
        if (mat == null) return;

        // Set warna dengan alpha
        if (mat.HasProperty("_Color"))
        {
            Color c = mat.color;
            c.a = alpha;
            mat.color = c;
        }
        else if (mat.HasProperty("_BaseColor")) // URP/HDRP
        {
            Color c = mat.GetColor("_BaseColor");
            c.a = alpha;
            mat.SetColor("_BaseColor", c);
        }

        string shaderName = mat.shader != null ? mat.shader.name : "";

        if (shaderName.Contains("Universal Render Pipeline"))
        {
            // URP Lit
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // 0=Opaque, 1=Transparent
            mat.SetOverrideTag("RenderType", "Transparent");

            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f); // Alpha (opsional)

            mat.SetInt("_ZWrite", 0);

            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");

            mat.renderQueue = (int)RenderQueue.Transparent;
        }
        else
        {
            // Built-in Standard
            if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 3f); // 3 = Transparent
            mat.SetOverrideTag("RenderType", "Transparent");

            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);

            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");

            mat.renderQueue = (int)RenderQueue.Transparent;
        }
    }
}
