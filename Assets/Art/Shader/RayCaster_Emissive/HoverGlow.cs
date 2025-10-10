using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class HoverGlow : MonoBehaviour
{
    [Header("Shader Property")]
    [SerializeField] private string glowPropName = "_BE_GlowIntensity"; // Must match the Shader Reference name (with underscore)
    private int glowPropID;

    [Header("Intensity")]
    public float minIntensity = 0f;   // Target value when not hovered
    public float maxIntensity = 2f;   // Target value when hovered

    [Header("Timing (seconds)")]
    public float riseDuration = 0.5f; // Time to rise from min -> max
    public float fallDuration = 0.5f; // Time to fall from max -> min

    private Renderer _renderer;
    private MaterialPropertyBlock _mpb;

    private float _current; // Current glow intensity
    private float _target;  // Target glow intensity
    private bool _hovered;  // Whether currently hovered

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();
        glowPropID = Shader.PropertyToID(glowPropName);

        // Initialize the value to minIntensity
        _renderer.GetPropertyBlock(_mpb);
        _current = _mpb.GetFloat(glowPropID);
        if (float.IsNaN(_current)) _current = minIntensity;

        _target = minIntensity;
        Apply();
    }

    void Update()
    {
        // Determine which duration to use depending on hover state
        float duration = _hovered ? Mathf.Max(0.0001f, riseDuration)
                                  : Mathf.Max(0.0001f, fallDuration);

        float speed = Mathf.Abs(maxIntensity - minIntensity) / duration;
        _current = Mathf.MoveTowards(_current, _target, speed * Time.deltaTime);
        Apply();
    }

    // Apply the updated glow value to the material property block
    void Apply()
    {
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(glowPropID, _current);
        _renderer.SetPropertyBlock(_mpb);
    }

    // Called by the raycaster when the mouse enters or exits
    public void SetHovered(bool hovered)
    {
        _hovered = hovered;
        _target = _hovered ? maxIntensity : minIntensity;
    }
}
