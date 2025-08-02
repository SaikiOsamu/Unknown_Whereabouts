using UnityEngine;

public class Ground3D : MonoBehaviour
{
    private bool onGround;
    private float friction;

    private void OnCollisionEnter(Collision collision)
    {
        EvaluateCollision(collision);
        RetrieveFriction(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        EvaluateCollision(collision);
        RetrieveFriction(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        onGround = false;
        friction = 0;
    }

    private void EvaluateCollision(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            if (normal.y >= 0.9f)
            {
                onGround = true;
                break;
            }
        }
    }

    private void RetrieveFriction(Collision collision)
    {
        friction = 0;

        foreach (ContactPoint contact in collision.contacts)
        {
            Collider col = contact.otherCollider;
            if (col != null && col.sharedMaterial != null)
            {
                float matFriction = col.sharedMaterial.dynamicFriction;
                if (matFriction > friction)
                {
                    friction = matFriction;
                }
            }
        }
    }

    public bool GetOnGround()
    {
        return onGround;
    }

    public float GetFriction()
    {
        return friction;
    }
}
