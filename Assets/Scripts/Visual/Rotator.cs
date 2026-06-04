using UnityEngine;

public class Rotator : MonoBehaviour
{
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0f, 0f, 0f);
    [SerializeField] private bool preserveSize = false;

    private Vector3 originalLocalScale;
    private float originalWorldExtentX;
    private float originalWorldExtentY;
    private float accumulatedZAngle;

    private void Start()
    {
        originalLocalScale = transform.localScale;
        // Use lossyScale so world-space parent scaling is accounted for
        // (handles both root objects and child mesh objects correctly).
        originalWorldExtentX = Mathf.Abs(transform.lossyScale.x);
        originalWorldExtentY = Mathf.Abs(transform.lossyScale.y);
        accumulatedZAngle = 0f;
    }

    private void Update()
    {
        accumulatedZAngle += rotationSpeed.z * Time.deltaTime;
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);

        if (preserveSize)
            ApplySizePreservation();
    }

    private void ApplySizePreservation()
    {
        float rad = accumulatedZAngle * Mathf.Deg2Rad;
        float c = Mathf.Abs(Mathf.Cos(rad));
        float s = Mathf.Abs(Mathf.Sin(rad));

        // Apparent projected width when rotated by θ = extentX·|cosθ| + extentY·|sinθ|.
        // Scale XY uniformly so apparent width stays equal to original width.
        // Z is left unchanged — depth doesn't affect the orthographic silhouette.
        float apparentWidth = originalWorldExtentX * c + originalWorldExtentY * s;
        float uniformScale = apparentWidth > 0f ? originalWorldExtentX / apparentWidth : 1f;
        transform.localScale = new Vector3(
            originalLocalScale.x * uniformScale,
            originalLocalScale.y * uniformScale,
            originalLocalScale.z);
    }
}