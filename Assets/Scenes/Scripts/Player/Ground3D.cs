using UnityEngine;

public class Ground3D : MonoBehaviour
{
    private bool onGround;

    private void OnCollisionEnter(Collision collision)
    {
        EvaluateCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        EvaluateCollision(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        onGround = false;
    }

    private void EvaluateCollision(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;

            if (normal.y >= 0.9f)
            {
                onGround = true;
                return;
            }
        }
        onGround = false;
    }

    public bool GetOnGround()
    {
        return onGround;
    }
}
