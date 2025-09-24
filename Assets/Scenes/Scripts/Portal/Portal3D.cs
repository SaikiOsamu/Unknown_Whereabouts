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

    [Header("Visuals (Optional)")]
    public Material activeMaterial;
    public Material cooldownMaterial;
    public List<Renderer> renderers = new List<Renderer>();

    private bool playerIsInside = false;
    private Transform player;
    private Vector3 entryLocalPosition;
    private bool hasTeleported = false;
    private float lastTeleportTime = -Mathf.Infinity;
    private Collider portalCollider;

    void Awake()
    {
        portalCollider = GetComponent<Collider>();
        if (renderers == null || renderers.Count == 0)
        {
            var r = GetComponent<Renderer>();
            if (r != null) renderers = new List<Renderer> { r };
        }
        ApplyCooldownState(IsOnCooldown());
    }

    void OnEnable()
    {
        ApplyCooldownState(IsOnCooldown());
    }

    void Update()
    {
        ApplyCooldownState(IsOnCooldown());

        if (!playerIsInside || player == null || hasTeleported)
            return;

        Vector3 currentLocalPos = transform.InverseTransformPoint(player.position);
        float penetrationDistance = Vector3.Distance(currentLocalPos, entryLocalPosition);

        if (!IsOnCooldown() && portalCollider != null && portalCollider.isTrigger)
        {
            if (penetrationDistance >= triggerDepth)
            {
                TeleportPlayer();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsValidPlayer(other)) return;
        if (IsOnCooldown()) return;

        playerIsInside = true;
        player = other.transform;
        entryLocalPosition = transform.InverseTransformPoint(player.position);
        hasTeleported = false;
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsValidPlayer(other)) return;

        playerIsInside = false;
        player = null;
    }

    void TeleportPlayer()
    {
        if (receiverPortal == null || player == null) return;

        lastTeleportTime = Time.time;
        Portal3D receiver = receiverPortal.GetComponent<Portal3D>();
        if (receiver != null && receiver.exitPoint != null)
        {
            player.position = receiver.exitPoint.position;
        }
        else
        {
            player.position = receiverPortal.position;
        }

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

    private bool IsOnCooldown()
    {
        return (Time.time - lastTeleportTime) < teleportCooldown;
    }

    private bool IsValidPlayer(Collider other)
    {
        return other.CompareTag("Player");
    }

    private void ApplyCooldownState(bool cooling)
    {
        if (portalCollider != null)
        {
            portalCollider.isTrigger = !cooling;
        }

        if (renderers != null && renderers.Count > 0)
        {
            Material targetMat = cooling ? cooldownMaterial : activeMaterial;
            if (targetMat != null)
            {
                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    var mats = r.materials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        mats[i] = targetMat;
                    }
                    r.materials = mats;
                }
            }
        }
    }
}
