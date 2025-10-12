using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectRotationController : MonoBehaviour
{
    public float rotationSpeed = 100f;
    public Transform rotationCenter;

    public List<Transform> objectsToRotateWithRoom;
    public List<MonoBehaviour> inputControllersToDisable;

    public bool useRotationCurve = false;
    public AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private bool canRotateX = true;
    private bool canRotateY = true;
    private bool isRotating = false;

    private struct RigidbodyState
    {
        public bool isKinematic;
        public bool useGravity;
        public RigidbodyConstraints constraints;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public CollisionDetectionMode detectionMode;
        public RigidbodyInterpolation interpolation;
    }

    private readonly Dictionary<Rigidbody, RigidbodyState> _cachedRbStates = new Dictionary<Rigidbody, RigidbodyState>();

    void Start()
    {
        if (rotationCenter == null)
        {
            Debug.LogError("Rotation center is not assigned.");
            return;
        }
    }

    void Update()
    {
        if (rotationCenter == null || isRotating) return;

        float horizontal = Input.GetAxisRaw("Horizontal_Object_X");
        float vertical = Input.GetAxisRaw("Vertical_Object_Y");

        if (vertical > 0 && canRotateX)
        {
            StartCoroutine(RotateSmoothly(Vector3.right, 90f));
            AudioManager.Instance.Play("Rotate_SFX");
            canRotateX = false;
        }
        else if (vertical < 0 && canRotateX)
        {
            StartCoroutine(RotateSmoothly(Vector3.left, 90f));
            AudioManager.Instance.Play("Rotate_SFX");
            canRotateX = false;
        }

        if (horizontal > 0 && canRotateY)
        {
            StartCoroutine(RotateSmoothly(Vector3.up, 90f));
            AudioManager.Instance.Play("Rotate_SFX");
            canRotateY = false;
        }
        else if (horizontal < 0 && canRotateY)
        {
            StartCoroutine(RotateSmoothly(Vector3.down, 90f));
            AudioManager.Instance.Play("Rotate_SFX");
            canRotateY = false;
        }

        if (vertical == 0) canRotateX = true;
        if (horizontal == 0) canRotateY = true;
    }

    IEnumerator RotateSmoothly(Vector3 axis, float angle)
    {
        isRotating = true;

        foreach (var controller in inputControllersToDisable)
        {
            if (controller != null) controller.enabled = false;
        }

        FreezeRigidbodies();

        float duration = angle / Mathf.Max(0.0001f, rotationSpeed);
        float elapsed = 0f;
        float prevT = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float prevEval = useRotationCurve && rotationCurve != null ? rotationCurve.Evaluate(prevT) : prevT;
            float nowEval = useRotationCurve && rotationCurve != null ? rotationCurve.Evaluate(t) : t;
            float step = (nowEval - prevEval) * angle;

            transform.RotateAround(rotationCenter.position, axis, step);
            foreach (Transform obj in objectsToRotateWithRoom)
            {
                if (obj != null) obj.RotateAround(rotationCenter.position, axis, step);
            }

            prevT = t;
            yield return null;
        }

        if (prevT < 1f)
        {
            float remaining = (1f - (useRotationCurve && rotationCurve != null ? rotationCurve.Evaluate(prevT) : prevT)) * angle;
            transform.RotateAround(rotationCenter.position, axis, remaining);
            foreach (Transform obj in objectsToRotateWithRoom)
            {
                if (obj != null) obj.RotateAround(rotationCenter.position, axis, remaining);
            }
        }

        RestoreRigidbodies();

        foreach (var controller in inputControllersToDisable)
        {
            if (controller != null) controller.enabled = true;
        }

        isRotating = false;
    }

    private void FreezeRigidbodies()
    {
        _cachedRbStates.Clear();

        if (objectsToRotateWithRoom == null) return;

        foreach (var t in objectsToRotateWithRoom)
        {
            if (t == null) continue;

            var rb = t.GetComponent<Rigidbody>();
            if (rb == null) continue;

            if (_cachedRbStates.ContainsKey(rb)) continue;

            var state = new RigidbodyState
            {
                isKinematic = rb.isKinematic,
                useGravity = rb.useGravity,
                constraints = rb.constraints,
                velocity = rb.linearVelocity,
                angularVelocity = rb.angularVelocity,
                detectionMode = rb.collisionDetectionMode,
                interpolation = rb.interpolation
            };

            _cachedRbStates.Add(rb, state);

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    private void RestoreRigidbodies()
    {
        foreach (var kv in _cachedRbStates)
        {
            var rb = kv.Key;
            var state = kv.Value;
            if (rb == null) continue;

            rb.isKinematic = state.isKinematic;
            rb.useGravity = state.useGravity;
            rb.constraints = state.constraints;
            rb.collisionDetectionMode = state.detectionMode;
            rb.interpolation = state.interpolation;
            rb.linearVelocity = state.velocity;
            rb.angularVelocity = state.angularVelocity;
        }

        _cachedRbStates.Clear();
    }
}
