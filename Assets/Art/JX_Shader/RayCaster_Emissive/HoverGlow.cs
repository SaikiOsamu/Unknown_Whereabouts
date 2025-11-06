using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class HoverGlow : MonoBehaviour
{
    [Header("Shader Property")]
    [SerializeField] private string glowPropName = "_BE_GlowIntensity";
    private int glowPropID;

    [Header("Hover Intensities")]
    public float minIntensity = 0f;     // fallback when breath disabled
    public float maxIntensity = 2f;     // target when hovered

    [Header("Hover Timing (seconds)")]
    public float riseDuration = 0.5f;   // to max when hovered
    public float fallDuration = 0.5f;   // back to min if breath disabled

    [Header("Breath (when NOT hovered)")]
    public bool enableBreath = true;
    public float breathMin = 0f;
    public float breathMax = 2f;
    [Tooltip("Time for a full up-and-down (min→max→min), NOT including the peak hold.")]
    public float breathPeriod = 2.0f;
    [Tooltip("Hold time at peak intensity (seconds). 0 = no hold.")]
    public float breathPeakHold = 0.0f;
    [Tooltip("Easing applied to the 0..1 phase (e.g., S-curve).")]
    public AnimationCurve breathCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [Tooltip("How quickly current value follows the breath target (seconds).")]
    public float breathFollowDuration = 0.25f;
    [Tooltip("Optional per-object phase offset (0..1 of the full cycle). 0 keeps all perfectly in sync.")]
    [Range(0f, 1f)] public float breathPhaseOffset = 0f;

    private Renderer _renderer;
    private MaterialPropertyBlock _mpb;
    private float _current;
    private float _target;
    private bool _hovered;

    // ── Global phase state so every instance stays in sync ──────────────
    static bool s_phaseInited = false;
    static float s_phaseStartTime = 0f; // Time.time when breath started

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();
        glowPropID = Shader.PropertyToID(glowPropName);

        _renderer.GetPropertyBlock(_mpb);
        _current = _mpb.GetFloat(glowPropID);
        if (float.IsNaN(_current)) _current = minIntensity;

        if (!s_phaseInited) { s_phaseInited = true; s_phaseStartTime = Time.time; }

        if (breathMax < breathMin) breathMax = breathMin;     // guard
        if (Mathf.Approximately(breathMax, 0f) && Mathf.Approximately(breathMin, 0f))
            breathMax = maxIntensity;

        _target = _current;
        Apply();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        if (_hovered)
        {
            _target = maxIntensity;
            float dur = Mathf.Max(0.0001f, riseDuration);
            float spd = Mathf.Abs(maxIntensity - minIntensity) / dur;
            _current = Mathf.MoveTowards(_current, _target, spd * dt);
            Apply();
            return; // hovering does NOT change global phase; sync is preserved
        }

        if (enableBreath && breathPeriod > 0.0001f)
        {
            // Full cycle length = up (T/2) + peakHold + down (T/2)
            float half = breathPeriod * 0.5f;
            float cycleLen = half + breathPeakHold + half;

            // Global time-based phase keeps all instances in sync.
            float t0 = s_phaseStartTime;
            // add per-object offset in seconds (0..1 of cycle)
            float offsetSec = breathPhaseOffset * cycleLen;
            float t = (Time.time - t0 + offsetSec) % cycleLen;

            float phase01; // 0..1..0 triangle with a flat top if peak hold > 0
            if (t < half)
            {
                // rising 0 -> 1
                phase01 = t / half;
            }
            else if (t < half + breathPeakHold)
            {
                // hold at 1
                phase01 = 1f;
            }
            else
            {
                // falling 1 -> 0
                float downT = (t - half - breathPeakHold);
                phase01 = 1f - (downT / half);
            }

            float eased = breathCurve != null ? Mathf.Clamp01(breathCurve.Evaluate(phase01)) : phase01;
            float breathTarget = Mathf.Lerp(breathMin, breathMax, eased);
            _target = breathTarget;

            float dur = Mathf.Max(0.0001f, breathFollowDuration);
            float spd = Mathf.Abs(breathMax - breathMin) / dur;
            _current = Mathf.MoveTowards(_current, _target, spd * dt);
        }
        else
        {
            _target = minIntensity;
            float dur = Mathf.Max(0.0001f, fallDuration);
            float spd = Mathf.Abs(maxIntensity - minIntensity) / dur;
            _current = Mathf.MoveTowards(_current, _target, spd * dt);
        }

        Apply();
    }

    void Apply()
    {
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(glowPropID, _current);
        _renderer.SetPropertyBlock(_mpb);
    }

    // Called by HoverRaycaster
    public void SetHovered(bool hovered) => _hovered = hovered;
}
