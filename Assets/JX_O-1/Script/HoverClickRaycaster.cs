using UnityEngine;

public class HoverClickRaycaster : MonoBehaviour
{
    public Camera cam;
    public float maxDistance = 1000f;
    public LayerMask hitLayers = ~0;
    public bool requireFrontHover = true;
    [Range(0f, 1f)] public float frontDotThreshold = 0.2f;

    Ui3DStyleV2 _current;

    void Start() { if (!cam) cam = Camera.main; }

    void Update()
    {
        if (!cam) return;
        Ui3DStyleV2 hit = null;

        var ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var info, maxDistance, hitLayers, QueryTriggerInteraction.Ignore))
        {
            if (!requireFrontHover || Vector3.Dot(info.normal, -ray.direction) >= frontDotThreshold)
                hit = info.collider.GetComponentInParent<Ui3DStyleV2>();
        }

        if (hit != _current)
        {
            if (_current) _current.SetHovered(false);
            _current = hit;
            if (_current) _current.SetHovered(true);
        }

        if (_current != null && Input.GetMouseButtonDown(0))
            _current.ToggleSelected();
    }
}
