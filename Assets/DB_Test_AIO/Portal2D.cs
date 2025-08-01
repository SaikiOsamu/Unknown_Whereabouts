using UnityEngine;

public class Portal2D : MonoBehaviour
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

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time - lastTeleportTime < teleportCooldown) return;

        playerIsInside = true;
        player = other.transform;
        entryLocalPosition = transform.InverseTransformPoint(player.position);
        hasTeleported = false;
    }

    void OnTriggerExit2D(Collider2D other)
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
        float penetrationDistance = Vector2.Distance(
            new Vector2(currentLocalPos.x, currentLocalPos.y),
            new Vector2(entryLocalPosition.x, entryLocalPosition.y)
        );

        if (penetrationDistance >= triggerDepth)
        {
            TeleportPlayer();
        }
    }

    void TeleportPlayer()
    {
        if (receiverPortal == null || player == null) return;

        lastTeleportTime = Time.time;

        Portal2D receiver = receiverPortal.GetComponent<Portal2D>();
        if (receiver != null && receiver.exitPoint != null)
        {
            player.position = receiver.exitPoint.position;
        }
        else
        {
            player.position = receiverPortal.position; // fallback
        }

        // 通知目标门进入冷却
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
