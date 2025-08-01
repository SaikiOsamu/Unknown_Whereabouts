using UnityEngine;

public class Move3D : MonoBehaviour
{
    [SerializeField] private InputController input = null;
    [SerializeField, Range(0f, 100f)] private float maxSpeed = 4f;
    [SerializeField, Range(0f, 100f)] private float maxAcceleration = 35f;
    [SerializeField, Range(0f, 100f)] private float maxAirAcceleration = 20f;

    private Vector3 direction;
    private Vector3 desiredVelocity;
    private Vector3 velocity;
    private Rigidbody body;
    private Ground3D ground;

    private float maxSpeedChange;
    private float acceleration;
    private bool onGround;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        ground = GetComponent<Ground3D>();
    }

    void Update()
    {
        direction.x = input.RetrieveMoveInput();  // Assuming left/right only
        desiredVelocity = new Vector3(direction.x, 0f, 0f) * Mathf.Max(maxSpeed - ground.GetFriction(), 0f);
    }

    private void FixedUpdate()
    {
        onGround = ground.GetOnGround();
        velocity = body.linearVelocity;

        acceleration = onGround ? maxAcceleration : maxAirAcceleration;
        maxSpeedChange = acceleration * Time.deltaTime;
        velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);

        body.linearVelocity = velocity;
    }
}
