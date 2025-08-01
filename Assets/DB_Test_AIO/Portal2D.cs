using UnityEngine;

public class Portal2D : MonoBehaviour
{
    public Transform receiverPortal;
    public float triggerDepth = 0.6f;

    private bool playerIsInside = false;
    private Transform player;

    private Vector3 entryLocalPosition;
    private bool hasTeleported = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsInside = true;
            player = other.transform;

            // 将玩家位置转换为局部坐标，用于比较
            entryLocalPosition = transform.InverseTransformPoint(player.position);
            hasTeleported = false;
        }
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

        Vector3 localPos = transform.InverseTransformPoint(player.position);

        // 2D 中通常使用 Y 轴作为“前后”方向
        if (localPos.y > entryLocalPosition.y + triggerDepth)
        {
            TeleportPlayer();
        }
    }

    void TeleportPlayer()
    {
        if (receiverPortal == null) return;

        // 让玩家出现在接收门的位置，略偏前方
        Vector3 offset = receiverPortal.up * -0.5f;
        player.position = receiverPortal.position + offset;

        hasTeleported = true;
    }
}
