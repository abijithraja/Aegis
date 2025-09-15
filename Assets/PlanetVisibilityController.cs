using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class PlanetVisibilityController : MonoBehaviour
{
    [Header("References")]
    public Transform shipTransform;
    public Camera targetCamera;

    [Header("Visibility settings")]
    public float worldVisibilityDistance = 1200f;
    public bool useBillboardFallback = true;
    public float billboardDistanceFromCamera = 1800f;
    public float billboardScale = 1000f;

    [Header("Robustness")]
    public string[] disableComponentNameContains = new string[] { "PlanetLOD", "DisableIfFar", "Culled", "LODGroup" };

    List<Renderer> _renderers = new List<Renderer>();
    List<Behaviour> _disabledBehaviours = new List<Behaviour>();
    GameObject _billboardInstance;
    bool _initialized = false;

    void Awake()
    {
        _renderers.AddRange(GetComponentsInChildren<Renderer>(true));
        if (targetCamera == null) targetCamera = Camera.main;
        _initialized = true;
    }

    void Start()
    {
        DisableSuspectComponents();
        EnsureRenderersEnabled(true);
        if (!useBillboardFallback) EnsureRenderersEnabled(true);
    }

    void Update()
    {
        if (!_initialized) Awake();
        if (shipTransform == null)
        {
            var s = GameObject.Find("Ship");
            if (s != null) shipTransform = s.transform;
        }
        if (shipTransform == null) { EnsureRenderersEnabled(true); return; }

        float dist = Vector3.Distance(shipTransform.position, transform.position);

        if (useBillboardFallback && dist > worldVisibilityDistance)
        {
            if (_billboardInstance == null) CreateBillboard();
            UpdateBillboardTransform();
            EnsureRenderersEnabled(false);
        }
        else
        {
            if (_billboardInstance != null) DestroyBillboard();
            EnsureRenderersEnabled(true);
        }
    }

    void EnsureRenderersEnabled(bool enabled)
    {
        for (int i = 0; i < _renderers.Count; ++i)
            if (_renderers[i] != null && _renderers[i].enabled != enabled)
                _renderers[i].enabled = enabled;
    }

    void DisableSuspectComponents()
    {
        foreach (var beh in GetComponentsInChildren<Behaviour>(true))
        {
            if (beh == null) continue;
            string name = beh.GetType().Name;
            foreach (var key in disableComponentNameContains)
            {
                if (!string.IsNullOrEmpty(key) && name.IndexOf(key, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (beh.enabled)
                    {
                        beh.enabled = false;
                        _disabledBehaviours.Add(beh);
                        Debug.Log($"[PlanetVisibilityController] Disabled '{name}' on '{beh.gameObject.name}'.");
                    }
                }
            }
        }
        var lod = GetComponentInChildren<LODGroup>();
        if (lod != null) { lod.enabled = false; Debug.Log("[PlanetVisibilityController] Disabled LODGroup."); }
    }

    void CreateBillboard()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) { Debug.LogWarning("[PlanetVisibilityController] No camera found."); return; }

        _billboardInstance = new GameObject($"{gameObject.name}_Billboard");
        _billboardInstance.transform.SetParent(null);

        Renderer srcR = null;
        Mesh srcMesh = null;
        Material mat = null;

        if (_renderers.Count > 0 && _renderers[0] != null)
        {
            srcR = _renderers[0];

            var mf = srcR.GetComponent<MeshFilter>();
            if (mf != null) srcMesh = mf.sharedMesh;

            mat = srcR.sharedMaterial;
        }

        if (srcMesh == null)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            DestroyImmediate(quad.GetComponent<Collider>());
            quad.name = _billboardInstance.name + "_Quad";
            quad.transform.SetParent(_billboardInstance.transform, false);
            if (mat != null) quad.GetComponent<Renderer>().sharedMaterial = mat;
        }
        else
        {
            var clone = new GameObject(_billboardInstance.name + "_Mesh");
            var mf = clone.AddComponent<MeshFilter>();
            mf.sharedMesh = srcMesh;
            var mr = clone.AddComponent<MeshRenderer>();
            if (mat != null) mr.sharedMaterial = mat;
            clone.transform.SetParent(_billboardInstance.transform, false);
        }

        UpdateBillboardTransform();
    }

    void UpdateBillboardTransform()
    {
        if (_billboardInstance == null || targetCamera == null) return;
        Transform camT = targetCamera.transform;
        Vector3 pos = camT.position + camT.forward * billboardDistanceFromCamera;
        _billboardInstance.transform.position = pos;
        _billboardInstance.transform.rotation = Quaternion.LookRotation(_billboardInstance.transform.position - camT.position, Vector3.up);
        _billboardInstance.transform.localScale = Vector3.one * billboardScale;
    }

    void DestroyBillboard()
    {
        if (_billboardInstance != null) { Destroy(_billboardInstance); _billboardInstance = null; }
    }

    void OnDestroy()
    {
        foreach (var b in _disabledBehaviours) if (b != null) b.enabled = true;
        DestroyBillboard();
    }
}
