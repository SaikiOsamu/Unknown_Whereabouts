using UnityEngine;
using System.Collections;

public class ObjectRotationController_Unable : MonoBehaviour
{
    public float rotationSpeed = 100f;  // Controls rotation speed
    public float dampingFactor = 0.1f;  // Controls damping factor for smoother rotation and return motion
    public Transform rotationCenter;  // The center object around which this object rotates
    public float rotationAngle = 30f;  // Fixed rotation angle per input

    private bool canRotateX = true;  // Whether X-axis rotation is allowed
    private bool canRotateY = true;  // Whether Y-axis rotation is allowed
    private bool isRotating = false;  // Whether the object is currently rotating

    void Start()
    {
        if (rotationCenter == null)
        {
            Debug.LogError("Rotation center is not assigned.");
            return;
        }
    }

    void Update()
    {
        // Skip if rotation center is missing or the object is already rotating
        if (rotationCenter == null || isRotating) return;

        // Get directional input (using GetAxisRaw for immediate response)
        float horizontal = Input.GetAxisRaw("Horizontal_Object_X");  // Left and right arrow keys
        float vertical = Input.GetAxisRaw("Vertical_Object_Y");  // Up and down arrow keys

        // Handle up/down input for X-axis rotation
        if (vertical > 0 && canRotateX)  // Up key
        {
            StartCoroutine(RotateAndReturn(Vector3.right, rotationAngle));  // Rotate +30 degrees around X
            canRotateX = false;  // Disable further X rotation until input is released
        }
        else if (vertical < 0 && canRotateX)  // Down key
        {
            StartCoroutine(RotateAndReturn(Vector3.left, rotationAngle));  // Rotate -30 degrees around X
            canRotateX = false;  // Disable further X rotation until input is released
        }

        // Handle left/right input for Y-axis rotation
        if (horizontal > 0 && canRotateY)  // Right key
        {
            StartCoroutine(RotateAndReturn(Vector3.up, rotationAngle));  // Rotate +30 degrees around Y
            canRotateY = false;  // Disable further Y rotation until input is released
        }
        else if (horizontal < 0 && canRotateY)  // Left key
        {
            StartCoroutine(RotateAndReturn(Vector3.down, rotationAngle));  // Rotate -30 degrees around X
            canRotateY = false;  // Disable further Y rotation until input is released
        }

        // Reset rotation permissions when input is released
        if (vertical == 0)
        {
            canRotateX = true;
        }

        if (horizontal == 0)
        {
            canRotateY = true;
        }
    }

    IEnumerator RotateAndReturn(Vector3 axis, float angle)
    {
        isRotating = true;  // 标记为正在旋转

        // 保存原始旋转
        Quaternion originalRotation = transform.rotation;

        // 旋转到目标角度
        float elapsedTime = 0f;
        float targetAngle = angle;

        // 目标旋转角度
        Quaternion targetRotation = Quaternion.Euler(axis * targetAngle) * transform.rotation;

        // 旋转到目标角度，使用缓动来平滑
        while (elapsedTime < 1f)
        {
            float smoothSpeed = Mathf.SmoothStep(0f, rotationSpeed, elapsedTime); // 使用缓动函数控制速度
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);
            elapsedTime += Time.deltaTime * dampingFactor;
            yield return null;
        }

        // 确保旋转到目标角度
        transform.rotation = targetRotation;

        // 等待一定时间，然后返回原始角度
        yield return new WaitForSeconds(0.5f);  // 延迟 0.5 秒，控制回弹等待时间

        // 旋转回原始角度
        elapsedTime = 0f;

        // 使用缓动回弹效果
        while (elapsedTime < 1f)
        {
            float smoothSpeed = Mathf.SmoothStep(0f, rotationSpeed, elapsedTime); // 使用缓动函数控制回弹速度
            transform.rotation = Quaternion.Slerp(transform.rotation, originalRotation, smoothSpeed * Time.deltaTime);
            elapsedTime += Time.deltaTime * dampingFactor;
            yield return null;
        }

        // 确保回到原始旋转
        transform.rotation = originalRotation;

        isRotating = false;  // 旋转完成，重置标志
    }
}
