using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Renderer))]
public class PlaneBreak : MonoBehaviour
{
    public Material planeMaterial;
    public GameObject TriggerObject;
    public GameObject TriggerZone;
    [Range(0f, 1f)] public float requiredOverlapFraction = 2f / 3f;
    public float transparencyDuration = 3f;

    public GameObject PlayerKillZone;
    public string playerTag = "Player";

    private Renderer doorRenderer;
    private Collider doorCollider;
    private Collider triggerObjectCollider;
    private Collider triggerZoneCollider;

    private float timeElapsed = 0f;
    private bool isFadingIn = false;
    private bool hasActivated = false;

    private class TriggerForwarder : MonoBehaviour
    {
        public PlaneBreak owner;
        private void OnTriggerEnter(Collider other)
        {
            if (owner != null) owner.OnKillZoneTriggerEnter(other);
        }
    }

    void Awake()
    {
        doorRenderer = GetComponent<Renderer>();
        if (planeMaterial == null && doorRenderer != null) planeMaterial = doorRenderer.material;
        doorCollider = GetComponent<Collider>();
        if (TriggerObject != null) triggerObjectCollider = TriggerObject.GetComponent<Collider>();
        if (TriggerZone != null) triggerZoneCollider = TriggerZone.GetComponent<Collider>();

        if (PlayerKillZone != null)
        {
            var col = PlayerKillZone.GetComponent<Collider>();
            if (col == null) col = PlayerKillZone.AddComponent<BoxCollider>();
            col.isTrigger = true;

            var fwd = PlayerKillZone.GetComponent<TriggerForwarder>();
            if (fwd == null) fwd = PlayerKillZone.AddComponent<TriggerForwarder>();
            fwd.owner = this;
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
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
            if (fraction >= requiredOverlapFraction) ActivateDoor();
        }
    }

    void ActivateDoor()
    {
        hasActivated = true;
        timeElapsed = 0f;
        SetTransparency(0f);
        isFadingIn = true;
       
        AudioManager.Instance.PlaySequenceScheduled(0.10, "ExitShown_SFX", "TriggerZone_PartOne", "TriggerZone_PartTwo");


        if (doorCollider != null) doorCollider.enabled = true;
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

    private void OnKillZoneTriggerEnter(Collider other)
    {
        if (string.IsNullOrEmpty(playerTag) || other.CompareTag(playerTag))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AudioManager.Instance.Stop("TriggerZone_PartTwo");
    }
}
