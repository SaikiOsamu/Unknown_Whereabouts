using UnityEngine;

public class PlaneBreak : MonoBehaviour
{
    [Header("Material Settings")]
    public Material planeMaterial;  // �����ڱ༭����ѡ�����

    [Header("Additional GameObject")]
    public GameObject targetObject;  // �ڱ༭����ѡ���Ŀ�� GameObject

    [Header("Transparency Settings")]
    public float transparencyDuration = 3f;  // ��͸���ĳ���ʱ��

    private Renderer planeRenderer;
    private int clickCount = 0;
    //private float transparencyStep = 0.33f; // ÿ�ε��ʱ͸���ȼ��ٵı���
    private float timeElapsed = 0f; // ���ڼ�¼��͸����ʱ��
    private bool isFading = false; // �Ƿ����ڱ�͸��

    void Start()
    {
        if (planeMaterial == null)
        {
            // ���û��ָ�����ʣ���ʹ�õ�ǰ�����Ĭ�ϲ���
            planeRenderer = GetComponent<Renderer>();
            planeMaterial = planeRenderer.material;
        }

        // ȷ������ʹ��͸��ģʽ
        if (planeMaterial != null && planeMaterial.HasProperty("_Mode"))
        {
            planeMaterial.SetFloat("_Mode", 3);  // ����Ϊ͸��ģʽ
            planeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);  // ���û��ģʽ
            planeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);  // ����Ŀ����ģʽ
            planeMaterial.SetInt("_ZWrite", 0);  // �ر����д��
            planeMaterial.DisableKeyword("_ALPHATEST_ON");
            planeMaterial.EnableKeyword("_ALPHABLEND_ON");
            planeMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            planeMaterial.renderQueue = 3000;  // ��Ⱦ��������Ϊ͸������
        }

        // ȷ������һ��ʼ����ȫ��͸����
        SetTransparency(1f);
    }

    void Update()
    {
        // ��������
        if (Input.GetMouseButtonDown(0)) // 0��������
        {
            HandleClick();
        }

        // ������ڱ�͸��������͸����
        if (isFading)
        {
            timeElapsed += Time.deltaTime;
            float newAlpha = Mathf.Lerp(1f, 0f, timeElapsed / transparencyDuration);  // ���㵱ǰ͸����

            // ���²��ʵ�͸����
            SetTransparency(newAlpha);

            // ���͸�����Ѿ���ȫ��ʧ����ݻ�����
            if (newAlpha <= 0f)
            {
                Destroy(gameObject); // ���ٸ�����
            }
        }
    }

    void HandleClick()
    {
        // ��һ�ε��ʱ��ʼ��͸��
        if (clickCount == 0)
        {
            isFading = true;  // ����͸���ȱ仯
            timeElapsed = 0f;  // ����ʱ��
        }

        // ���ӵ������
        clickCount++;

        // ��һ�ε��ʱ������Ŀ�����
        if (clickCount == 1 && targetObject != null)
        {
            targetObject.SetActive(true);  // ����Ŀ�� GameObject
        }
    }

    // ���ò���͸���ȵĸ�������
    void SetTransparency(float alpha)
    {
        Color currentColor = planeMaterial.color;
        currentColor.a = alpha;
        planeMaterial.color = currentColor;
    }
}
