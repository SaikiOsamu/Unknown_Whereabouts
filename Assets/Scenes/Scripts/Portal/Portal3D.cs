using UnityEngine;

public class Portal3D : MonoBehaviour
{
    [Header("Portal Settings")]
    public Transform receiverPortal;
    public float triggerDepth = 0.6f;
    public float teleportCooldown = 0.5f;

    [Tooltip("Exact destination point after teleport (set to empty GameObject)")]
    public Transform exitPoint;

    private bool playerIsInside = false;
    private Transform player;

    private Vector3 entryLocalPosition;
    private bool hasTeleported = false;
    private float lastTeleportTime = -Mathf.Infinity;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time - lastTeleportTime < teleportCooldown) return;

        playerIsInside = true;
        player = other.transform;
        entryLocalPosition = transform.InverseTransformPoint(player.position);
        hasTeleported = false;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsInside = false;
            player = null;
        }
    }

    void Update()
    {
        if (!playerIsInside || player == null || hasTeleported)
            return;

        Vector3 currentLocalPos = transform.InverseTransformPoint(player.position);
        float penetrationDistance = Vector3.Distance(currentLocalPos, entryLocalPosition);

        if (penetrationDistance >= triggerDepth)
        {
            TeleportPlayer();
        }
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
            player.position = receiverPortal.position; // fallback
        }

        if (receiver != null)
        {
            receiver.ForceCooldown();
        }

        hasTeleported = true;
    }

    public void ForceCooldown()
    {
        lastTeleportTime = Time.time;
    }
}
