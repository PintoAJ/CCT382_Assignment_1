using UnityEngine;

[DisallowMultipleComponent]
public class MenuCameraParallax : MonoBehaviour
{
    [Header("Pan limits (world units)")]
    [SerializeField] Vector2 maxPan = new Vector2(0.8f, 0.5f); // X=left/right, Y=up/down

    [Header("Tilt limits (degrees)")]
    [SerializeField, Range(0f, 20f)] float maxTilt = 6f;       // yaw/pitch tilt

    [Header("Smoothing")]
    [SerializeField, Range(0.01f, 1f)] float smoothTime = 0.18f;
    [SerializeField] bool useUnscaledTime = true;

    [Header("Feel")]
    [SerializeField] bool invertX = false;  // flip horizontal feel if desired
    [SerializeField] bool invertY = false;  // flip vertical feel if desired
    [SerializeField] bool lockZ = true;     // keep rig Z fixed

    Vector3 baseLocalPos;
    Quaternion baseLocalRot;

    Vector3 vel; // for SmoothDamp

    void Awake()
    {
        // Save the rig's starting pose (camera should be a child with local zeroed)
        baseLocalPos = transform.localPosition;
        baseLocalRot = transform.localRotation;
    }

    void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        // Mouse position normalized to [-1, +1] around screen center
        Vector2 m = Input.mousePosition;
        float nx = (m.x / Mathf.Max(1f, Screen.width) - 0.5f) * 2f;
        float ny = (m.y / Mathf.Max(1f, Screen.height) - 0.5f) * 2f;

        if (invertX) nx = -nx;
        if (invertY) ny = -ny;

        // Target pan (local space)
        Vector3 targetLocalPos = baseLocalPos + new Vector3(
            nx * maxPan.x,
            ny * maxPan.y,
            0f
        );

        if (lockZ) targetLocalPos.z = baseLocalPos.z;

        // Smooth pan
        transform.localPosition = Vector3.SmoothDamp(
            transform.localPosition,
            targetLocalPos,
            ref vel,
            smoothTime,
            Mathf.Infinity,
            dt
        );

        // Target tilt (yaw from X, pitch from Y)
        float yaw = nx * maxTilt;  // rotate around Y
        float pitch = -ny * maxTilt;  // rotate around X (negative = look up when mouse goes up)

        Quaternion targetLocalRot =
            Quaternion.Euler(pitch, yaw, 0f) * baseLocalRot;

        // Smooth tilt
        transform.localRotation =
            Quaternion.Slerp(transform.localRotation, targetLocalRot, 1f - Mathf.Exp(-8f * dt));
    }

    // Optional toggle for when you later open panels or start Play
    public void SetEnabled(bool enabledParallax)
    {
        enabled = enabledParallax;
        if (!enabledParallax)
        {
            // snap back to base pose when disabled
            transform.localPosition = baseLocalPos;
            transform.localRotation = baseLocalRot;
        }
    }
}
