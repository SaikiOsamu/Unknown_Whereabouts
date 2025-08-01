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

            // �����λ��ת��Ϊ�ֲ����꣬���ڱȽ�
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

        // 2D ��ͨ��ʹ�� Y ����Ϊ��ǰ�󡱷���
        if (localPos.y > entryLocalPosition.y + triggerDepth)
        {
            TeleportPlayer();
        }
    }

    void TeleportPlayer()
    {
        if (receiverPortal == null) return;

        // ����ҳ����ڽ����ŵ�λ�ã���ƫǰ��
        Vector3 offset = receiverPortal.up * -0.5f;
        player.position = receiverPortal.position + offset;

        hasTeleported = true;
    }
}
