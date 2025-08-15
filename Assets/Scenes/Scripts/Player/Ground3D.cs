using UnityEngine;

public class Ground3D : MonoBehaviour
{
    [Range(0f, 89f)]
    public float maxGroundAngle = 50f;
    private float minGroundDot;
    private bool groundedThisFrame;
    private bool onGround;

    void Awake()
    {
        minGroundDot = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }

    void FixedUpdate()
    {
        onGround = groundedThisFrame;
        groundedThisFrame = false;
    }

    void OnCollisionEnter(Collision c) { EvaluateCollision(c); }
    void OnCollisionStay(Collision c) { EvaluateCollision(c); }
    void OnCollisionExit(Collision c) { }

    void EvaluateCollision(Collision c)
    {
        for (int i = 0; i < c.contactCount; i++)
        {
            Vector3 n = c.GetContact(i).normal;
            if (n.y >= minGroundDot)
            {
                groundedThisFrame = true;
                break;
            }
        }
    }

    public bool GetOnGround() => onGround;
}
