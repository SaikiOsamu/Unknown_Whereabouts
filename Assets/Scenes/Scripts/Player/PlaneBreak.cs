using UnityEngine;

public class PlaneBreak : MonoBehaviour
{
    [Header("Material Settings")]
    public Material planeMaterial;  // 让你在编辑器中选择材质

    [Header("Additional GameObject")]
    public GameObject targetObject;  // 在编辑器中选择的目标 GameObject

    [Header("Transparency Settings")]
    public float transparencyDuration = 3f;  // 变透明的持续时间

    private Renderer planeRenderer;
    private int clickCount = 0;
    //private float transparencyStep = 0.33f; // 每次点击时透明度减少的比例
    private float timeElapsed = 0f; // 用于记录变透明的时间
    private bool isFading = false; // 是否正在变透明

    void Start()
    {
        if (planeMaterial == null)
        {
            // 如果没有指定材质，则使用当前物体的默认材质
            planeRenderer = GetComponent<Renderer>();
            planeMaterial = planeRenderer.material;
        }

        // 确保材质使用透明模式
        if (planeMaterial != null && planeMaterial.HasProperty("_Mode"))
        {
            planeMaterial.SetFloat("_Mode", 3);  // 设置为透明模式
            planeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);  // 设置混合模式
            planeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);  // 设置目标混合模式
            planeMaterial.SetInt("_ZWrite", 0);  // 关闭深度写入
            planeMaterial.DisableKeyword("_ALPHATEST_ON");
            planeMaterial.EnableKeyword("_ALPHABLEND_ON");
            planeMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            planeMaterial.renderQueue = 3000;  // 渲染队列设置为透明物体
        }

        // 确保物体一开始是完全不透明的
        SetTransparency(1f);
    }

    void Update()
    {
        // 检测鼠标点击
        if (Input.GetMouseButtonDown(0)) // 0是鼠标左键
        {
            HandleClick();
        }

        // 如果正在变透明，更新透明度
        if (isFading)
        {
            timeElapsed += Time.deltaTime;
            float newAlpha = Mathf.Lerp(1f, 0f, timeElapsed / transparencyDuration);  // 计算当前透明度

            // 更新材质的透明度
            SetTransparency(newAlpha);

            // 如果透明度已经完全消失，则摧毁物体
            if (newAlpha <= 0f)
            {
                Destroy(gameObject); // 销毁该物体
            }
        }
    }

    void HandleClick()
    {
        // 第一次点击时开始变透明
        if (clickCount == 0)
        {
            isFading = true;  // 启动透明度变化
            timeElapsed = 0f;  // 重置时间
        }

        // 增加点击次数
        clickCount++;

        // 第一次点击时，激活目标对象
        if (clickCount == 1 && targetObject != null)
        {
            targetObject.SetActive(true);  // 激活目标 GameObject
        }
    }

    // 设置材质透明度的辅助方法
    void SetTransparency(float alpha)
    {
        Color currentColor = planeMaterial.color;
        currentColor.a = alpha;
        planeMaterial.color = currentColor;
    }
}
