using UnityEngine;

public class LaserDraw : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;

    // 在 Inspector 可调
    public float startWidth = 0.2f;
    public float endWidth   = 0.2f;

    LineRenderer laserLine;

    void Awake()
    {
        laserLine = GetComponent<LineRenderer>();
    }

    void Start()
    {
        laserLine.positionCount = 2;

        laserLine.startWidth = startWidth;
        laserLine.endWidth   = endWidth;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!laserLine) laserLine = GetComponent<LineRenderer>();
        if (!laserLine) return;
        laserLine.startWidth = startWidth;
        laserLine.endWidth   = endWidth;
    }
#endif

    void Update()
    {
        laserLine.SetPosition(0, startPoint.position);
        laserLine.SetPosition(1, endPoint.position);
    }
}
