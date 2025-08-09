using UnityEngine;

public class Move3D : MonoBehaviour
{
    [SerializeField] private InputController input = null;

    [Header("Speed")]
    [SerializeField, Range(0f, 50f)] private float maxSpeed = 6f;        // 最大移动速度

    [Header("Smoothing")]
    [SerializeField, Range(0f, 200f)] private float startStep = 60f;     // 起步/加速时每秒速度变化量
    [SerializeField, Range(0f, 200f)] private float stopStep = 80f;      // 停止/松手时每秒速度衰减量
    [SerializeField, Range(0f, 0.2f)] private float sleepThreshold = 0.01f; // 速度很小时直接归零，防抖

    private Rigidbody body;
    private float inputX;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        //body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        body.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void Update()
    {
        inputX = input != null ? input.RetrieveMoveInput() : 0f;
    }

    void FixedUpdate()
    {
        Vector3 v = body.linearVelocity;
        if (Mathf.Abs(inputX) > 0.001f)
        {
            float targetX = inputX * maxSpeed;
            v.x = Mathf.MoveTowards(v.x, targetX, startStep * Time.fixedDeltaTime);
        }
        else
        {
            v.x = Mathf.MoveTowards(v.x, 0f, stopStep * Time.fixedDeltaTime);

            if (Mathf.Abs(v.x) < sleepThreshold) v.x = 0f;
        }

        body.linearVelocity = v;
    }
}
