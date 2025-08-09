using UnityEngine;

public class Move3D : MonoBehaviour
{
    [SerializeField] private InputController input = null;
    [SerializeField, Range(0f, 50f)] private float maxSpeed = 6f;
    [SerializeField, Range(0f, 200f)] private float startStep = 60f;
    [SerializeField, Range(0f, 200f)] private float stopStep = 80f;
    [SerializeField, Range(0f, 0.2f)] private float sleepThreshold = 0.01f;

    private Rigidbody body;
    private float inputX;
    private float currentSpeedX;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
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
    }
}
