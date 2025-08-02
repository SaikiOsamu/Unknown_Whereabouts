using UnityEngine;

public class Jump3D : MonoBehaviour
{
    [SerializeField] private InputController input = null;
    [SerializeField, Range(0f, 10f)] private float jumpHeight = 3f;
    [SerializeField, Range(0, 5)] private int maxAirJumps = 0;
    [SerializeField, Range(0f, 5f)] private float downwardMovementMultiplier = 3f;
    [SerializeField, Range(0f, 5f)] private float upwardMovementMultiplier = 1.7f;

    private Rigidbody body;
    private Ground3D ground;
    private Vector3 velocity;

    private int jumpPhase;
    //private float defaultGravityScale = 1f;

    private bool desiredJump;
    private bool onGround;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        ground = GetComponent<Ground3D>();
    }

    void Update()
    {
        desiredJump |= input.RetrieveJumpInput();
    }

    private void FixedUpdate()
    {
        onGround = ground.GetOnGround();
        velocity = body.linearVelocity;

        if (onGround)
        {
            jumpPhase = 0;
        }

        if (desiredJump)
        {
            desiredJump = false;
            JumpAction();
        }

        if (body.linearVelocity.y > 0)
        {
            body.linearVelocity += Vector3.up * Physics.gravity.y * (upwardMovementMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (body.linearVelocity.y < 0)
        {
            body.linearVelocity += Vector3.up * Physics.gravity.y * (downwardMovementMultiplier - 1f) * Time.fixedDeltaTime;
        }

        body.linearVelocity = velocity;
    }

    private void JumpAction()
    {
        if (onGround || jumpPhase < maxAirJumps)
        {
            jumpPhase += 1;
            float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
            if (velocity.y > 0f)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - velocity.y, 0f);
            }
            velocity.y += jumpSpeed;
        }
    }
}
