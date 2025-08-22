using UnityEngine;

public class KeepFeetDown : MonoBehaviour
{
    public Ground3D ground;
    public Rigidbody rb;
    [Range(0f, 1080f)] public float alignSpeedDegPerSec = 540f;

    void Reset()
    {
        ground = GetComponentInChildren<Ground3D>();
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (ground != null && ground.GetOnGround()) return;

        Vector3 g = Physics.gravity;
        if (g.sqrMagnitude < 1e-6f) return;

        Vector3 upTarget = -g.normalized;
        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, upTarget);
        if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.Cross(transform.right, upTarget);

        Quaternion target = Quaternion.LookRotation(fwd.normalized, upTarget);
        Quaternion next = Quaternion.RotateTowards(transform.rotation, target, alignSpeedDegPerSec * Time.fixedDeltaTime);

        if (rb != null && !rb.isKinematic) rb.MoveRotation(next);
        else transform.rotation = next;
    }
}
