using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Shaker : MonoBehaviour
{
    [Header("Shake Settings")]
    public float shakeAmount = 0.1f;         // ��������
    public float shakeSpeed = 20f;           // �����ٶ�
    public float shakeDuration = 0.3f;       // ÿ�ζ�������ʱ��

    [Header("Activation Settings")]
    public string targetSceneName = "DB_Test";  // �޶�������

    private bool shakeEnabled = false;      // �Ƿ�������������&ʱ���жϣ�
    private bool isShaking = false;         // ��ǰ�Ƿ����ڶ���
    private Vector3 originalPosition;

    void Start()
    {
        // �����жϣ�1������ö���
        if (SceneManager.GetActiveScene().name == targetSceneName)
        {
            StartCoroutine(EnableShakeAfterDelay(1f));
        }
    }

    IEnumerator EnableShakeAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        shakeEnabled = true;
    }

    void Update()
    {
        if (!shakeEnabled || isShaking) return;

        // �������� W �� S ʱ��������
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S))
        {
            StartCoroutine(ShakeOnce());
        }
    }

    IEnumerator ShakeOnce()
    {
        isShaking = true;

        //�ڶ�����ʼʱ��¼��ǰλ�ã�֧�ֽ�ɫ�ƶ���
        originalPosition = transform.localPosition;

        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float offsetX = (Mathf.PerlinNoise(Time.time * shakeSpeed, 0f) - 0.5f) * 2f;
            float offsetY = (Mathf.PerlinNoise(0f, Time.time * shakeSpeed) - 0.5f) * 2f;
            Vector3 shakeOffset = new Vector3(offsetX, offsetY, 0f) * shakeAmount;

            transform.localPosition = originalPosition + shakeOffset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // �ָ�����ǰ��λ��
        transform.localPosition = originalPosition;
        isShaking = false;
    }
}
