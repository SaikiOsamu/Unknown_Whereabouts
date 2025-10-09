using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(Collider))]
public class PlaneBreak : MonoBehaviour
{
    [Header("References")]
    public GameObject TriggerObject;
    public GameObject TriggerZone;

    [Header("Trigger Condition (same as before)")]
    [Range(0f, 1f)] public float requiredOverlapFraction = 2f / 3f;

    [Header("Kill Zone (optional)")]
    public GameObject PlayerKillZone;
    public string playerTag = "Player";

    [Header("Noise Control (Shader property)")]
    public string noisePropertyName = "_NoiseScale";
    public float preTouchNoise = 20f;
    public float postTouchNoise = 0f;
    public Transform visualsRoot;
    public bool includeInactiveChildren = true;

    private Renderer doorRenderer;
    private Collider doorCollider;
    private Collider triggerObjectCollider;
    private Collider triggerZoneCollider;

    private bool hasActivated = false;
    private int noisePropertyID;

    private readonly List<Renderer> _renderers = new List<Renderer>();
    private readonly Dictionary<(Renderer r, int matIndex), MaterialPropertyBlock> _mpbCache =
        new Dictionary<(Renderer, int), MaterialPropertyBlock>(64);

    private class TriggerForwarder : MonoBehaviour
    {
        public PlaneBreak owner;
        private void OnTriggerEnter(Collider other)
        {
            if (owner != null) owner.OnKillZoneTriggerEnter(other);
        }
    }

    void Awake()
    {
        doorRenderer = GetComponent<Renderer>();
        doorCollider = GetComponent<Collider>();

        if (TriggerObject != null) triggerObjectCollider = TriggerObject.GetComponent<Collider>();
        if (TriggerZone != null) triggerZoneCollider = TriggerZone.GetComponent<Collider>();

        if (PlayerKillZone != null)
        {
            var col = PlayerKillZone.GetComponent<Collider>();
            if (col == null) col = PlayerKillZone.AddComponent<BoxCollider>();
            col.isTrigger = true;

            var fwd = PlayerKillZone.GetComponent<TriggerForwarder>();
            if (fwd == null) fwd = PlayerKillZone.AddComponent<TriggerForwarder>();
            fwd.owner = this;
        }

        noisePropertyID = Shader.PropertyToID(noisePropertyName);
        Transform root = visualsRoot == null ? transform : visualsRoot;
        _renderers.Clear();
        root.GetComponentsInChildren(includeInactiveChildren, _renderers);
        FilterRenderersByProperty(_renderers, noisePropertyID);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        ApplyNoise(preTouchNoise);
        if (doorRenderer != null) doorRenderer.enabled = true;

        if (doorCollider != null)
        {
            doorCollider.enabled = true;
            doorCollider.isTrigger = false;
        }
    }

    void Update()
    {
        if (hasActivated) return;
        if (triggerObjectCollider == null || triggerZoneCollider == null) return;

        float fraction = ComputeOverlapFraction(triggerObjectCollider.bounds, triggerZoneCollider.bounds);
        if (fraction >= requiredOverlapFraction)
        {
            ActivateDoor();
        }
    }

    void ActivateDoor()
    {
        hasActivated = true;
        ApplyNoise(postTouchNoise);

        if (doorCollider != null)
        {
            doorCollider.enabled = true;
            doorCollider.isTrigger = true;
        }

        AudioManager.Instance.PlaySequenceScheduled(0.10, "ExitShown_SFX", "TriggerZone_PartOne", "TriggerZone_PartTwo");
    }

    void ApplyNoise(float value)
    {
        if (_renderers.Count == 0) return;

        foreach (var r in _renderers)
        {
            if (r == null) continue;
            var mats = r.sharedMaterials;
            int matCount = mats != null ? mats.Length : 0;
            for (int i = 0; i < matCount; i++)
            {
                var key = (r, i);
                if (!_mpbCache.TryGetValue(key, out var mpb))
                {
                    mpb = new MaterialPropertyBlock();
                    _mpbCache[key] = mpb;
                }
                r.GetPropertyBlock(mpb, i);
                mpb.SetFloat(noisePropertyID, value);
                r.SetPropertyBlock(mpb, i);
            }
        }
    }

    float ComputeOverlapFraction(Bounds objectBounds, Bounds zoneBounds)
    {
        float ix = Mathf.Max(0f, Mathf.Min(objectBounds.max.x, zoneBounds.max.x) - Mathf.Max(objectBounds.min.x, zoneBounds.min.x));
        float iy = Mathf.Max(0f, Mathf.Min(objectBounds.max.y, zoneBounds.max.y) - Mathf.Max(objectBounds.min.y, zoneBounds.min.y));
        float iz = Mathf.Max(0f, Mathf.Min(objectBounds.max.z, zoneBounds.max.z) - Mathf.Max(objectBounds.min.z, zoneBounds.min.z));
        float intersectionVolume = ix * iy * iz;
        float zoneVolume = zoneBounds.size.x * zoneBounds.size.y * zoneBounds.size.z;
        if (zoneVolume <= 0f) return 0f;
        return Mathf.Clamp01(intersectionVolume / zoneVolume);
    }

    private void OnKillZoneTriggerEnter(Collider other)
    {
        if (string.IsNullOrEmpty(playerTag) || other.CompareTag(playerTag))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AudioManager.Instance.Stop("TriggerZone_PartTwo");
    }

    static void FilterRenderersByProperty(List<Renderer> renderers, int propertyId)
    {
        for (int i = renderers.Count - 1; i >= 0; i--)
        {
            var r = renderers[i];
            if (r == null)
            {
                renderers.RemoveAt(i);
                continue;
            }
            var mats = r.sharedMaterials;
            bool ok = false;
            if (mats != null)
            {
                for (int m = 0; m < mats.Length; m++)
                {
                    var mat = mats[m];
                    if (mat != null && mat.HasProperty(propertyId))
                    {
                        ok = true;
                        break;
                    }
                }
            }
            if (!ok) renderers.RemoveAt(i);
        }
    }
}
