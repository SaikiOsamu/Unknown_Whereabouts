using UnityEngine;
using System.Collections;

public class ObjectRotationController : MonoBehaviour
{
    public float rotationSpeed = 100f;  // 控制旋转速度
    public Transform rotationCenter;  // 旋转中心物体

    private bool canRotateX = true;  // 判断是否可以进行X轴旋转
    private bool canRotateY = true;  // 判断是否可以进行Y轴旋转

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
        if (rotationCenter == null) return;  // 如果没有旋转中心则跳出

        // 获取输入的方向键（使用 GetAxisRaw 来确保按键按下后即触发）
        float horizontal = Input.GetAxisRaw("Horizontal");  // 左右箭头键
        float vertical = Input.GetAxisRaw("Vertical");  // 上下箭头键

        // 处理上下方向的旋转（X轴旋转）
        if (vertical > 0 && canRotateX)  // 按上键
        {
            StartCoroutine(RotateSmoothly(Vector3.right, 90f));  // X轴顺时针旋转
            canRotateX = false;  // 禁止继续旋转直到输入变化
        }
        else if (vertical < 0 && canRotateX)  // 按下键
        {
            StartCoroutine(RotateSmoothly(Vector3.left, 90f));  // X轴逆时针旋转
            canRotateX = false;  // 禁止继续旋转直到输入变化
        }

        // 处理左右方向的旋转（Y轴旋转）
        if (horizontal > 0 && canRotateY)  // 按右键
        {
            StartCoroutine(RotateSmoothly(Vector3.up, 90f));  // Y轴顺时针旋转
            canRotateY = false;  // 禁止继续旋转直到输入变化
        }
        else if (horizontal < 0 && canRotateY)  // 按左键
        {
            StartCoroutine(RotateSmoothly(Vector3.down, 90f));  // Y轴逆时针旋转
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

    IEnumerator RotateSmoothly(Vector3 axis, float angle)
    {
        float elapsedTime = 0f;
        float targetAngle = angle;

        // 旋转过程中逐渐增加旋转量
        Quaternion initialRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(axis * targetAngle) * rotationCenter.rotation;

        while (elapsedTime < targetAngle / rotationSpeed)
        {
            transform.RotateAround(rotationCenter.position, axis, rotationSpeed * Time.deltaTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
}
