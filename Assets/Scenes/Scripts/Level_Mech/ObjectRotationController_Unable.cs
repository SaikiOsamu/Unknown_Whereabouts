using UnityEngine;
using System.Collections;

public class ObjectRotationController_Unable : MonoBehaviour
{
    public float rotationSpeed = 100f;  // 控制旋转速度
    public float dampingFactor = 0.1f;  // 控制阻尼因子，使得旋转回弹过程更加平滑
    public Transform rotationCenter;  // 旋转中心物体
    public float rotationAngle = 30f;  // 固定旋转角度

    private bool canRotateX = true;  // 判断是否可以进行X轴旋转
    private bool canRotateY = true;  // 判断是否可以进行Y轴旋转
    private bool isRotating = false;  // 判断是否正在旋转

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
        if (rotationCenter == null || isRotating) return;  // 如果没有旋转中心或正在旋转则跳出

        // 获取输入的方向键（使用 GetAxisRaw 来确保按键按下后即触发）
        float horizontal = Input.GetAxisRaw("Horizontal_Object_X");  // 左右箭头键
        float vertical = Input.GetAxisRaw("Vertical_Object_Y");  // 上下箭头键

        // 处理上下方向的旋转（X轴旋转）
        if (vertical > 0 && canRotateX)  // 按上键
        {
            StartCoroutine(RotateAndReturn(Vector3.right, rotationAngle));  // 旋转 30度 顺时针
            canRotateX = false;  // 禁止继续旋转直到输入变化
        }
        else if (vertical < 0 && canRotateX)  // 按下键
        {
            StartCoroutine(RotateAndReturn(Vector3.left, rotationAngle));  // 旋转 30度 逆时针
            canRotateX = false;  // 禁止继续旋转直到输入变化
        }

        // 处理左右方向的旋转（Y轴旋转）
        if (horizontal > 0 && canRotateY)  // 按右键
        {
            StartCoroutine(RotateAndReturn(Vector3.up, rotationAngle));  // 旋转 30度 顺时针
            canRotateY = false;  // 禁止继续旋转直到输入变化
        }
        else if (horizontal < 0 && canRotateY)  // 按左键
        {
            StartCoroutine(RotateAndReturn(Vector3.down, rotationAngle));  // 旋转 30度 逆时针
            canRotateY = false;  // 禁止继续旋转直到输入变化
        }

        // 重置旋转状态，以便下次按键时可以旋转
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
