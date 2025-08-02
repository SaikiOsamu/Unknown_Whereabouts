using UnityEngine;
using System.Collections;

public class ObjectRotationController : MonoBehaviour
{
    public float rotationSpeed = 100f;  // ������ת�ٶ�
    public Transform rotationCenter;  // ��ת��������

    private bool canRotateX = true;  // �ж��Ƿ���Խ���X����ת
    private bool canRotateY = true;  // �ж��Ƿ���Խ���Y����ת
    private bool isRotating = false;  // �ж��Ƿ�������ת

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
        if (rotationCenter == null || isRotating) return;  // ���û����ת���Ļ�������ת������

        // ��ȡ����ķ������ʹ�� GetAxisRaw ��ȷ���������º󼴴�����
        float horizontal = Input.GetAxisRaw("Horizontal_Object_X");  // ���Ҽ�ͷ��
        float vertical = Input.GetAxisRaw("Vertical_Object_Y");  // ���¼�ͷ��

        // �������·������ת��X����ת��
        if (vertical > 0 && canRotateX)  // ���ϼ�
        {
            StartCoroutine(RotateSmoothly(Vector3.right, 90f));  // X��˳ʱ����ת
            canRotateX = false;  // ��ֹ������תֱ������仯
        }
        else if (vertical < 0 && canRotateX)  // ���¼�
        {
            StartCoroutine(RotateSmoothly(Vector3.left, 90f));  // X����ʱ����ת
            canRotateX = false;  // ��ֹ������תֱ������仯
        }

        // �������ҷ������ת��Y����ת��
        if (horizontal > 0 && canRotateY)  // ���Ҽ�
        {
            StartCoroutine(RotateSmoothly(Vector3.up, 90f));  // Y��˳ʱ����ת
            canRotateY = false;  // ��ֹ������תֱ������仯
        }
        else if (horizontal < 0 && canRotateY)  // �����
        {
            StartCoroutine(RotateSmoothly(Vector3.down, 90f));  // Y����ʱ����ת
            canRotateY = false;  // ��ֹ������תֱ������仯
        }

        // ������ת״̬���Ա��´ΰ���ʱ������ת
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
        isRotating = true;  // ���Ϊ������ת
        float elapsedTime = 0f;
        float targetAngle = angle;

        // ��ת��������������ת��
        Quaternion initialRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(axis * targetAngle) * rotationCenter.rotation;

        while (elapsedTime < targetAngle / rotationSpeed)
        {
            transform.RotateAround(rotationCenter.position, axis, rotationSpeed * Time.deltaTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // ȷ����ת��ɺ���ȫ����Ŀ��λ��
        transform.RotateAround(rotationCenter.position, axis, rotationSpeed * Time.deltaTime);

        isRotating = false;  // ��ת��ɣ����ñ�־
    }
}
