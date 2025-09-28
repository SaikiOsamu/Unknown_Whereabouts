using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class Portal3D : MonoBehaviour
{
    [Header("Portal Settings")]
    public Transform receiverPortal;
    public float triggerDepth = 0.6f;
    public float teleportCooldown = 0.5f;
    public Transform exitPoint;

    [Header("Visuals")]
    public List<Renderer> renderers = new List<Renderer>();
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

    void Awake()
    {
        portalCollider = GetComponent<Collider>();
        if (renderers == null || renderers.Count == 0)
        {
            var r = GetComponent<Renderer>();
            if (r != null) renderers = new List<Renderer> { r };
        }

        noiseScaleID = Shader.PropertyToID(noiseScaleRef);

        foreach (var r in renderers)
        {
            if (r == null) continue;
            var mats = r.materials;
            r.materials = mats;
        }

        ApplyCooldownState(IsOnCooldown());
    }

    void OnEnable() { ApplyCooldownState(IsOnCooldown()); }

    void Update()
    {
        ApplyCooldownState(IsOnCooldown());
        if (!playerIsInside || player == null || hasTeleported) return;

        Vector3 pLocal = transform.InverseTransformPoint(player.position);
        float pen = Vector3.Distance(pLocal, entryLocalPosition);
        if (!IsOnCooldown() && portalCollider != null && portalCollider.isTrigger && pen >= triggerDepth)
            TeleportPlayer();
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
        player.position = (receiver != null && receiver.exitPoint != null) ? receiver.exitPoint.position : receiverPortal.position;
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
        if (renderers == null || renderers.Count == 0) return;

        float ns = cooling ? cooldownNoiseScale : activeNoiseScale;

        foreach (var r in renderers)
        {
            if (r == null) continue;
            var mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null) continue;
                if (noiseScaleID != 0 && m.HasFloat(noiseScaleID)) m.SetFloat(noiseScaleID, ns);
            }
            r.materials = mats;
        }
    }
}
