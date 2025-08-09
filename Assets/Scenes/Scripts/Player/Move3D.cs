using UnityEngine;

public class Move3D : MonoBehaviour
{
    [SerializeField] private InputController input = null;

    [Header("Speed")]
    [SerializeField, Range(0f, 50f)] private float maxSpeed = 6f;        // ����ƶ��ٶ�

    [Header("Smoothing")]
    [SerializeField, Range(0f, 200f)] private float startStep = 60f;     // ��/����ʱÿ���ٶȱ仯��
    [SerializeField, Range(0f, 200f)] private float stopStep = 80f;      // ֹͣ/����ʱÿ���ٶ�˥����
    [SerializeField, Range(0f, 0.2f)] private float sleepThreshold = 0.01f; // �ٶȺ�Сʱֱ�ӹ��㣬����

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
