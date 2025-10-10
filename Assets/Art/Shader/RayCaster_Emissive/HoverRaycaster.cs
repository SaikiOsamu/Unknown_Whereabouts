using UnityEngine;

public class HoverRaycaster : MonoBehaviour
{
    public Camera cam;                   // Camera to cast rays from (default: MainCamera)
    public float maxDistance = 1000f;    // Maximum ray distance
    public LayerMask hitLayers = ~0;     // Layer mask for detectable objects

    private HoverGlow _current;          // Currently hovered object

    void Start()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        HoverGlow hitGlow = null;

        // Perform raycast
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, hitLayers, QueryTriggerInteraction.Ignore))
        {
            hitGlow = hit.collider.GetComponentInParent<HoverGlow>();
        }

        // Handle hover enter/exit transitions
        if (hitGlow != _current)
        {
            // If we moved off a previous object, tell it to stop glowing
            if (_current != null)
                _current.SetHovered(false);

            _current = hitGlow;

            // If we hit a new object, tell it to start glowing
            if (_current != null)
                _current.SetHovered(true);
        }
    }
}
