using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Shaker : MonoBehaviour
{
    [Header("Shake Settings")]
    public float shakeAmount = 0.1f;         // 抖动幅度
    public float shakeSpeed = 20f;           // 抖动速度
    public float shakeDuration = 0.3f;       // 每次抖动持续时间

    [Header("Activation Settings")]
    public string targetSceneName = "DB_Test";  // 限定场景名

    private bool shakeEnabled = false;      // 是否允许抖动（场景&时间判断）
    private bool isShaking = false;         // 当前是否正在抖动
    private Vector3 originalPosition;

    void Start()
    {
        // 场景判断，1秒后启用抖动
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

        // 仅当按下 W 或 S 时触发抖动
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S))
        {
            StartCoroutine(ShakeOnce());
        }
    }

    IEnumerator ShakeOnce()
    {
        isShaking = true;

        //在抖动开始时记录当前位置（支持角色移动）
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

        // 恢复抖动前的位置
        transform.localPosition = originalPosition;
        isShaking = false;
    }
}
