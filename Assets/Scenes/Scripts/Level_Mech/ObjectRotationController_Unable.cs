using UnityEngine;
using System.Collections;

public class ObjectRotationController_Unable : MonoBehaviour
{
    public float rotationSpeed = 100f;  // ������ת�ٶ�
    public float dampingFactor = 0.1f;  // �����������ӣ�ʹ����ת�ص����̸���ƽ��
    public Transform rotationCenter;  // ��ת��������
    public float rotationAngle = 30f;  // �̶���ת�Ƕ�

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
            StartCoroutine(RotateAndReturn(Vector3.right, rotationAngle));  // ��ת 30�� ˳ʱ��
            canRotateX = false;  // ��ֹ������תֱ������仯
        }
        else if (vertical < 0 && canRotateX)  // ���¼�
        {
            StartCoroutine(RotateAndReturn(Vector3.left, rotationAngle));  // ��ת 30�� ��ʱ��
            canRotateX = false;  // ��ֹ������תֱ������仯
        }

        // �������ҷ������ת��Y����ת��
        if (horizontal > 0 && canRotateY)  // ���Ҽ�
        {
            StartCoroutine(RotateAndReturn(Vector3.up, rotationAngle));  // ��ת 30�� ˳ʱ��
            canRotateY = false;  // ��ֹ������תֱ������仯
        }
        else if (horizontal < 0 && canRotateY)  // �����
        {
            StartCoroutine(RotateAndReturn(Vector3.down, rotationAngle));  // ��ת 30�� ��ʱ��
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

    IEnumerator RotateAndReturn(Vector3 axis, float angle)
    {
        isRotating = true;  // ���Ϊ������ת

        // ����ԭʼ��ת
        Quaternion originalRotation = transform.rotation;

        // ��ת��Ŀ��Ƕ�
        float elapsedTime = 0f;
        float targetAngle = angle;

        // Ŀ����ת�Ƕ�
        Quaternion targetRotation = Quaternion.Euler(axis * targetAngle) * transform.rotation;

        // ��ת��Ŀ��Ƕȣ�ʹ�û�����ƽ��
        while (elapsedTime < 1f)
        {
            float smoothSpeed = Mathf.SmoothStep(0f, rotationSpeed, elapsedTime); // ʹ�û������������ٶ�
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);
            elapsedTime += Time.deltaTime * dampingFactor;
            yield return null;
        }

        // ȷ����ת��Ŀ��Ƕ�
        transform.rotation = targetRotation;

        // �ȴ�һ��ʱ�䣬Ȼ�󷵻�ԭʼ�Ƕ�
        yield return new WaitForSeconds(0.5f);  // �ӳ� 0.5 �룬���ƻص��ȴ�ʱ��

        // ��ת��ԭʼ�Ƕ�
        elapsedTime = 0f;

        // ʹ�û����ص�Ч��
        while (elapsedTime < 1f)
        {
            float smoothSpeed = Mathf.SmoothStep(0f, rotationSpeed, elapsedTime); // ʹ�û����������ƻص��ٶ�
            transform.rotation = Quaternion.Slerp(transform.rotation, originalRotation, smoothSpeed * Time.deltaTime);
            elapsedTime += Time.deltaTime * dampingFactor;
            yield return null;
        }

        // ȷ���ص�ԭʼ��ת
        transform.rotation = originalRotation;

        isRotating = false;  // ��ת��ɣ����ñ�־
    }
}
