using UnityEngine;

public class HoverRaycaster : MonoBehaviour
{
    [Header("Raycast")]
    public Camera cam;
    public float maxDistance = 1000f;
    public LayerMask hitLayers = ~0;

    [Header("Front-Face Gating")]
    public bool requireFrontHover = true;
    [Range(0f, 1f)] public float frontDotThreshold = 0.2f;
    [Range(0.5f, 1f)] public float faceDotThreshold = 0.95f;
    public Transform frontAxis;
    public Vector3 localFront = Vector3.forward;

    private HoverGlow _current;

    void Start()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        HoverGlow hitGlow = null;

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, hitLayers, QueryTriggerInteraction.Ignore))
        {
            if (!requireFrontHover || IsFrontHit(ray, hit))
            {
                hitGlow = hit.collider.GetComponentInParent<HoverGlow>();
            }
        }

        if (hitGlow != _current)
        {
            if (_current != null)
                _current.SetHovered(false);

            _current = hitGlow;

            if (_current != null)
                _current.SetHovered(true);
        }
    }

    bool IsFrontHit(Ray ray, RaycastHit hit)
    {
        Vector3 rayDir = ray.direction.normalized;
        float facingDot = Vector3.Dot(hit.normal, -rayDir);
        if (facingDot < frontDotThreshold)
            return false;

        Transform basis = frontAxis ? frontAxis : hit.collider.transform;
        Vector3 worldFront = basis.TransformDirection(localFront).normalized;

        float faceDot = Vector3.Dot(hit.normal, worldFront);
        return faceDot >= faceDotThreshold;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Transform basis = frontAxis ? frontAxis : (cam != null ? cam.transform : transform);
        Vector3 worldFront = basis.TransformDirection(localFront).normalized;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(basis.position, basis.position + worldFront * 0.8f);
    }
#endif
}
