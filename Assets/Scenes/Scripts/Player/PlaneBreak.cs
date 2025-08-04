using UnityEngine;

public class PlaneBreak : MonoBehaviour
{
    [Header("Material Settings")]
    public Material planeMaterial;

    [Header("Additional GameObject")]
    public GameObject targetObject;

    [Header("Transparency Settings")]
    public float transparencyDuration = 3f;

    private Renderer planeRenderer;
    private int clickCount = 0;
    private float timeElapsed = 0f;
    private bool isFading = false;

    void Start()
    {
        if (planeMaterial == null)
        {
            planeRenderer = GetComponent<Renderer>();
            planeMaterial = planeRenderer.material;
        }

        if (planeMaterial != null && planeMaterial.HasProperty("_Mode"))
        {
            planeMaterial.SetFloat("_Mode", 3);
            planeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            planeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            planeMaterial.SetInt("_ZWrite", 0);
            planeMaterial.DisableKeyword("_ALPHATEST_ON");
            planeMaterial.EnableKeyword("_ALPHABLEND_ON");
            planeMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            planeMaterial.renderQueue = 3000;
        }

        SetTransparency(1f);
    }

    void Update()
    {
        // 用 Raycast 检测点击是否命中标签为 "Facade" 的物体
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray);

            foreach (RaycastHit hit in hits)
            {
                Debug.Log("Hit: " + hit.collider.name);

                if (hit.collider.CompareTag("Facade"))
                {
                    HandleClick();
                    break;
                }
            }
        }


        if (isFading)
        {
            timeElapsed += Time.deltaTime;
            float newAlpha = Mathf.Lerp(1f, 0f, timeElapsed / transparencyDuration);
            SetTransparency(newAlpha);

            if (newAlpha <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }

    void HandleClick()
    {
        if (clickCount == 0)
        {
            isFading = true;
            timeElapsed = 0f;
        }

        clickCount++;

        if (clickCount == 1 && targetObject != null)
        {
            targetObject.SetActive(true);
        }
    }

    void SetTransparency(float alpha)
    {
        Color currentColor = planeMaterial.color;
        currentColor.a = alpha;
        planeMaterial.color = currentColor;
    }
}
