using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class ViewBasedRayActivator : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public Transform target;
        public Transform frontAxis;
        public Vector3 localFront = Vector3.forward;
        public List<Renderer> renderers = new List<Renderer>();
        public List<GameObject> gameObjects = new List<GameObject>();
        [HideInInspector] public bool isFrontHidden;
        [HideInInspector] public Vector3 lastWorldCenter;
        [HideInInspector] public float lastCheckTime;
    }

    public Camera cam;
    public List<Entry> entries = new List<Entry>();
    [Range(0f, 90f)] public float frontEnterAngle = 30f;
    [Range(0f, 120f)] public float frontExitAngle = 40f;

    public bool requireInFrontOfCamera = true;
    public bool requireInViewport = false;
    public float maxCheckDistance = 200f;

    public bool useRayOcclusion = true;
    public LayerMask occlusionMask = ~0;
    [Range(0f, 0.5f)] public float sphereRadius = 0.05f;
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Range(1, 30)] public int framesInterval = 2;
    [Range(1, 128)] public int maxEntriesPerFrame = 24;
    public bool staggerByIndex = true;

    public bool useRendererBoundsCenter = true;
    public Vector3 pivotOffsetWS = Vector3.zero;

    public bool toggleGameObjects = true;
    public bool toggleRenderers = false;

    int frameCounter = 0;

    void Awake()
    {
        if (!cam) cam = Camera.main;
    }

    void Update()
    {
        if (entries == null || entries.Count == 0 || !cam) return;
        frameCounter++;
        int processed = 0;
        int startIndex = 0;
        if (staggerByIndex) startIndex = (frameCounter % framesInterval) % Mathf.Max(1, framesInterval);
        for (int i = startIndex; i < entries.Count; i += Mathf.Max(1, framesInterval))
        {
            if (processed >= maxEntriesPerFrame) break;
            ProcessEntry(entries[i]);
            processed++;
        }
    }

    void ProcessEntry(Entry e)
    {
        if (!e.target) return;
        Vector3 worldCenter;
        if (useRendererBoundsCenter && e.renderers != null && e.renderers.Count > 0 && e.renderers[0])
        {
            var bounds = new Bounds(e.renderers[0].bounds.center, Vector3.zero);
            for (int i = 0; i < e.renderers.Count; i++) if (e.renderers[i]) bounds.Encapsulate(e.renderers[i].bounds);
            worldCenter = bounds.center;
        }
        else worldCenter = e.target.position + pivotOffsetWS;

        Vector3 camPos = cam.transform.position;
        Vector3 toTarget = worldCenter - camPos;
        float sqrDist = toTarget.sqrMagnitude;

        if (maxCheckDistance > 0f && sqrDist > maxCheckDistance * maxCheckDistance)
        { SetActiveState(e, true); return; }

        if (requireInFrontOfCamera || requireInViewport)
        {
            Vector3 clip = cam.WorldToViewportPoint(worldCenter);
            if (requireInFrontOfCamera && clip.z <= 0f) { SetActiveState(e, true); return; }
            if (requireInViewport && (clip.x < 0f || clip.x > 1f || clip.y < 0f || clip.y > 1f)) { SetActiveState(e, true); return; }
        }

        Transform basis = e.frontAxis ? e.frontAxis : e.target;
        Vector3 worldFront = basis.TransformDirection(e.localFront).normalized;
        Vector3 dirToCamera = (camPos - worldCenter).normalized;
        float angle = Vector3.Angle(worldFront, dirToCamera);

        bool occluded = false;
        if (useRayOcclusion)
        {
            Vector3 rayOrigin = cam.transform.position;
            Vector3 rayDir = worldCenter - rayOrigin;
            float rayLen = rayDir.magnitude;
            if (rayLen > 0.0001f)
            {
                rayDir /= rayLen;
                if (sphereRadius > 0f)
                    occluded = Physics.SphereCast(rayOrigin, sphereRadius, rayDir, out _, rayLen - 0.01f, occlusionMask, triggerInteraction);
                else
                    occluded = Physics.Raycast(rayOrigin, rayDir, rayLen - 0.01f, occlusionMask, triggerInteraction);
            }
        }

        bool currentlyHidden = e.isFrontHidden;
        if (!currentlyHidden)
        {
            if (angle <= frontEnterAngle && !occluded) { e.isFrontHidden = true; ApplyVisibility(e, true); }
            else SetActiveState(e, true);
        }
        else
        {
            if (angle >= frontExitAngle || occluded) { e.isFrontHidden = false; SetActiveState(e, true); }
            else ApplyVisibility(e, true);
        }

        e.lastWorldCenter = worldCenter;
        e.lastCheckTime = Time.time;
    }

    void SetActiveState(Entry e, bool active)
    {
        if (toggleGameObjects && e.gameObjects != null)
            for (int i = 0; i < e.gameObjects.Count; i++)
                if (e.gameObjects[i] && e.gameObjects[i].activeSelf != active)
                    e.gameObjects[i].SetActive(active);

        if (toggleRenderers && e.renderers != null)
            for (int i = 0; i < e.renderers.Count; i++)
                if (e.renderers[i] && e.renderers[i].enabled != active)
                    e.renderers[i].enabled = active;
    }

    void ApplyVisibility(Entry e, bool hideFront)
    {
        bool active = !hideFront;
        SetActiveState(e, active);
    }
}
