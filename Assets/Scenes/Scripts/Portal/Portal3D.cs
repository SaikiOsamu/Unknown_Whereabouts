using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class Portal3D : MonoBehaviour
{
    [Header("Portal Settings")]
    public Transform receiverPortal;
    [Range(0f, 2f)] public float triggerDepth = 0.6f;
    [Range(0f, 3f)] public float teleportCooldown = 0.5f;
    public Transform exitPoint;

    [Header("Visuals (Auto-discovers children)")]
    public Transform visualsRoot;
    public bool includeInactiveChildren = true;

    public string noiseScaleRef = "_NoiseScale";
    public float activeNoiseScale = 20f;
    public float cooldownNoiseScale = 0f;

    private bool playerIsInside = false;
    private Transform player;
    private Vector3 entryLocalPosition;
    private bool hasTeleported = false;
    private float lastTeleportTime = -Mathf.Infinity;
    private Collider portalCollider;
    private int noiseScaleID;

    private readonly List<Renderer> _renderers = new List<Renderer>();
    private readonly Dictionary<(Renderer r, int matIndex), MaterialPropertyBlock> _mpbCache =
        new Dictionary<(Renderer, int), MaterialPropertyBlock>(64);

    void Awake()
    {
        portalCollider = GetComponent<Collider>();
        noiseScaleID = Shader.PropertyToID(noiseScaleRef);
        Transform root = visualsRoot == null ? transform : visualsRoot;
        _renderers.Clear();
        root.GetComponentsInChildren(includeInactiveChildren, _renderers);
        FilterRenderersByProperty(_renderers, noiseScaleID);
        ApplyCooldownState(IsOnCooldown());
    }

    void OnEnable() => ApplyCooldownState(IsOnCooldown());

    void Update()
    {
        ApplyCooldownState(IsOnCooldown());
        if (!playerIsInside || player == null || hasTeleported) return;
        Vector3 pLocal = transform.InverseTransformPoint(player.position);
        float pen = Vector3.Distance(pLocal, entryLocalPosition);
        if (!IsOnCooldown() && portalCollider != null && portalCollider.isTrigger && pen >= triggerDepth)
        {
            TeleportPlayer();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (IsOnCooldown()) return;
        playerIsInside = true;
        player = other.transform;
        entryLocalPosition = transform.InverseTransformPoint(player.position);
        hasTeleported = false;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerIsInside = false;
        player = null;
    }

    void TeleportPlayer()
    {
        if (receiverPortal == null || player == null) return;
        lastTeleportTime = Time.time;
        var receiver = receiverPortal.GetComponent<Portal3D>();
        player.position = (receiver != null && receiver.exitPoint != null)
            ? receiver.exitPoint.position
            : receiverPortal.position;
        ForceCooldown();
        if (receiver != null) receiver.ForceCooldown();
        hasTeleported = true;
        playerIsInside = false;
        player = null;
    }

    public void ForceCooldown()
    {
        lastTeleportTime = Time.time;
        ApplyCooldownState(true);
    }

    bool IsOnCooldown() => (Time.time - lastTeleportTime) < teleportCooldown;

    void ApplyCooldownState(bool cooling)
    {
        if (portalCollider != null) portalCollider.isTrigger = !cooling;
        if (_renderers.Count == 0) return;
        float ns = cooling ? cooldownNoiseScale : activeNoiseScale;
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
                mpb.SetFloat(noiseScaleID, ns);
                r.SetPropertyBlock(mpb, i);
            }
        }
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
