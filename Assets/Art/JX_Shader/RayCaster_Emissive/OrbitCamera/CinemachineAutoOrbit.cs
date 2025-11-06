using UnityEngine;

public class CinemachineAutoOrbit : MonoBehaviour
{
    public Transform target;          // 要围绕旋转的目标对象
    public float radius = 5f;         // 与目标的水平距离
    public float height = 2f;         // 相机在目标上方的高度
    public float rotationSpeed = 30f; // 每秒旋转的角速度（度/秒）

    private float currentAngle = 0f;

    void Update()
    {
        if (target == null) return;

        // 每帧增加角度
        currentAngle += rotationSpeed * Time.deltaTime;
        if (currentAngle > 360f) currentAngle -= 360f;

        // 计算旋转后的相机位置
        float rad = currentAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * radius;
        Vector3 desiredPosition = target.position + offset + Vector3.up * height;

        // 设置位置与朝向
        transform.position = desiredPosition;
        transform.LookAt(target.position);
    }
}
