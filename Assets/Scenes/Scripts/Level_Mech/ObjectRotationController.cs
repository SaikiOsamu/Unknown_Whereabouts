using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectRotationController : MonoBehaviour
{
    public float rotationSpeed = 100f;
    public Transform rotationCenter;

    public List<Transform> objectsToRotateWithRoom;  // 需要跟随旋转的对象（如角色）
    public List<MonoBehaviour> inputControllersToDisable;  // 角色控制脚本（如 PlayerController）

    private bool canRotateX = true;
    private bool canRotateY = true;
    private bool isRotating = false;

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
        if (rotationCenter == null || isRotating) return;

        float horizontal = Input.GetAxisRaw("Horizontal_Object_X");
        float vertical = Input.GetAxisRaw("Vertical_Object_Y");

        if (vertical > 0 && canRotateX)
        {
            StartCoroutine(RotateSmoothly(Vector3.right, 90f));
            canRotateX = false;
        }
        else if (vertical < 0 && canRotateX)
        {
            StartCoroutine(RotateSmoothly(Vector3.left, 90f));
            canRotateX = false;
        }

        if (horizontal > 0 && canRotateY)
        {
            StartCoroutine(RotateSmoothly(Vector3.up, 90f));
            canRotateY = false;
        }
        else if (horizontal < 0 && canRotateY)
        {
            StartCoroutine(RotateSmoothly(Vector3.down, 90f));
            canRotateY = false;
        }

        if (vertical == 0) canRotateX = true;
        if (horizontal == 0) canRotateY = true;
    }

    IEnumerator RotateSmoothly(Vector3 axis, float angle)
    {
        isRotating = true;

        // 禁用玩家输入
        foreach (var controller in inputControllersToDisable)
        {
            if (controller != null) controller.enabled = false;
        }

        float elapsedTime = 0f;
        float duration = angle / rotationSpeed;

        while (elapsedTime < duration)
        {
            float deltaAngle = rotationSpeed * Time.deltaTime;

            transform.RotateAround(rotationCenter.position, axis, deltaAngle);

            foreach (Transform obj in objectsToRotateWithRoom)
            {
                obj.RotateAround(rotationCenter.position, axis, deltaAngle);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 最后一帧补偿
        float remainingAngle = angle - rotationSpeed * elapsedTime;
        transform.RotateAround(rotationCenter.position, axis, remainingAngle);
        foreach (Transform obj in objectsToRotateWithRoom)
        {
            obj.RotateAround(rotationCenter.position, axis, remainingAngle);
        }

        // 恢复输入
        foreach (var controller in inputControllersToDisable)
        {
            if (controller != null) controller.enabled = true;
        }

        isRotating = false;
    }
}
