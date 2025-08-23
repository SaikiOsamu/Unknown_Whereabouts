using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class PlaneBreak : MonoBehaviour
{
    public Material planeMaterial;
    public GameObject TriggerObject;
    public GameObject TriggerZone;
    [Range(0f, 1f)] public float requiredOverlapFraction = 2f / 3f;
    public float transparencyDuration = 3f;

    private Renderer doorRenderer;
    private Collider doorCollider;
    private Collider triggerObjectCollider;
    private Collider triggerZoneCollider;

    private float timeElapsed = 0f;
    private bool isFadingIn = false;
    private bool hasActivated = false;

    void Awake()
    {
        doorRenderer = GetComponent<Renderer>();
        if (planeMaterial == null && doorRenderer != null) planeMaterial = doorRenderer.material;
        doorCollider = GetComponent<Collider>();
        if (TriggerObject != null) triggerObjectCollider = TriggerObject.GetComponent<Collider>();
        if (TriggerZone != null) triggerZoneCollider = TriggerZone.GetComponent<Collider>();
    }

    void Start()
    {
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
        SetTransparency(0f);
        if (doorCollider != null) doorCollider.enabled = false;
        if (doorRenderer != null) doorRenderer.enabled = true;
    }

    void Update()
    {
        if (isFadingIn)
        {
            timeElapsed += Time.deltaTime;
            float t = (transparencyDuration > 0f) ? Mathf.Clamp01(timeElapsed / transparencyDuration) : 1f;
            SetTransparency(Mathf.Lerp(0f, 1f, t));
            if (t >= 1f)
            {
                isFadingIn = false;
                SetTransparency(1f);
            }
            return;
        }

        if (!hasActivated && triggerObjectCollider != null && triggerZoneCollider != null)
        {
            float fraction = ComputeOverlapFraction(triggerObjectCollider.bounds, triggerZoneCollider.bounds);
            if (fraction >= requiredOverlapFraction) ActivateDoorAndRemoveRigidbody();
        }
    }

    void ActivateDoorAndRemoveRigidbody()
    {
        hasActivated = true;
        timeElapsed = 0f;
        SetTransparency(0f);
        isFadingIn = true;
        if (doorCollider != null) doorCollider.enabled = true;

        if (TriggerObject != null)
        {
            Rigidbody rb = TriggerObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                Destroy(rb);
            }
        }
    }

    void SetTransparency(float alpha)
    {
        if (planeMaterial == null) return;
        Color c = planeMaterial.color;
        c.a = Mathf.Clamp01(alpha);
        planeMaterial.color = c;
        if (doorRenderer != null && !doorRenderer.enabled && alpha > 0f) doorRenderer.enabled = true;
    }

    float ComputeOverlapFraction(Bounds objectBounds, Bounds zoneBounds)
    {
        float ix = Mathf.Max(0f, Mathf.Min(objectBounds.max.x, zoneBounds.max.x) - Mathf.Max(objectBounds.min.x, zoneBounds.min.x));
        float iy = Mathf.Max(0f, Mathf.Min(objectBounds.max.y, zoneBounds.max.y) - Mathf.Max(objectBounds.min.y, zoneBounds.min.y));
        float iz = Mathf.Max(0f, Mathf.Min(objectBounds.max.z, zoneBounds.max.z) - Mathf.Max(objectBounds.min.z, zoneBounds.min.z));
        float intersectionVolume = ix * iy * iz;
        float zoneVolume = zoneBounds.size.x * zoneBounds.size.y * zoneBounds.size.z;
        if (zoneVolume <= 0f) return 0f;
        return Mathf.Clamp01(intersectionVolume / zoneVolume);
    }
}
