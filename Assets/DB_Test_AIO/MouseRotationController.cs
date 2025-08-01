using UnityEngine;

public class MouseRotationController : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Rotation speed multiplier")]
    public float rotationSpeed = 5f;

    private float rotationX = 0f;
    private float rotationZ = 0f;

    void Update()
    {
        float mouseX = Input.GetAxis("Horizontal"); // 左右移动
        float mouseY = Input.GetAxis("Vertical"); // 上下移动

        rotationX -= mouseY * rotationSpeed;
        rotationZ += mouseX * rotationSpeed;

        transform.rotation = Quaternion.Euler(rotationX, 0f, rotationZ);
    }
}
