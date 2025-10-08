using UnityEngine;

public class Move3D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private InputController input = null;
    [SerializeField, Range(0f, 50f)] private float maxSpeed = 6f;
    [SerializeField, Range(0f, 200f)] private float startStep = 60f;
    [SerializeField, Range(0f, 200f)] private float stopStep = 80f;
    [SerializeField, Range(0f, 0.2f)] private float sleepThreshold = 0.01f;

    [Header("Facing")]
    [SerializeField] private Transform graphicsRoot;
    [SerializeField, Range(0f, 1440f)] private float turnSpeed = 720f;
    [SerializeField] private FacingAxis facingAxis = FacingAxis.Z;
    [SerializeField] private bool instantFace = false;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";
    [SerializeField, Range(0f, 1f)] private float speedDamp = 0.1f;

    private Rigidbody body;
    private float inputX;
    private float currentSpeedX;

    private enum FacingAxis { X, Z }

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        if (graphicsRoot == null) graphicsRoot = transform;
        if (animator == null)
        {
            if (graphicsRoot != null) animator = graphicsRoot.GetComponentInChildren<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }
    }

    void Update()
    {
        inputX = input != null ? input.RetrieveMoveInput() : 0f;
    }

    void FixedUpdate()
    {
        if (Mathf.Abs(inputX) > 0.001f)
        {
            float targetSpeed = inputX * maxSpeed;
            currentSpeedX = Mathf.MoveTowards(currentSpeedX, targetSpeed, startStep * Time.fixedDeltaTime);
        }
        else
        {
            currentSpeedX = Mathf.MoveTowards(currentSpeedX, 0f, stopStep * Time.fixedDeltaTime);
            if (Mathf.Abs(currentSpeedX) < sleepThreshold) currentSpeedX = 0f;
        }

        Vector3 position = body.position;
        position += new Vector3(currentSpeedX, 0f, 0f) * Time.fixedDeltaTime;
        body.MovePosition(position);

        float faceInput = Mathf.Abs(inputX) > 0.001f ? Mathf.Sign(inputX) : 0f;
        if (faceInput != 0f)
        {
            Vector3 desiredForward = (facingAxis == FacingAxis.Z)
                ? new Vector3(faceInput, 0f, 0f)
                : new Vector3(0f, 0f, faceInput);

            Quaternion targetRot = Quaternion.LookRotation(desiredForward, Vector3.up);

            if (instantFace)
            {
                if (graphicsRoot == transform) body.MoveRotation(targetRot);
                else graphicsRoot.rotation = targetRot;
            }
            else
            {
                float step = turnSpeed * Time.fixedDeltaTime;
                if (graphicsRoot == transform)
                {
                    Quaternion newRot = Quaternion.RotateTowards(body.rotation, targetRot, step);
                    body.MoveRotation(newRot);
                }
                else
                {
                    graphicsRoot.rotation = Quaternion.RotateTowards(graphicsRoot.rotation, targetRot, step);
                }
            }
        }

        if (animator != null && maxSpeed > 0f)
        {
            float speed01 = Mathf.Clamp01(Mathf.Abs(currentSpeedX) / maxSpeed);
            animator.SetFloat(speedParam, speed01, speedDamp, Time.fixedDeltaTime);
        }
    }
}
