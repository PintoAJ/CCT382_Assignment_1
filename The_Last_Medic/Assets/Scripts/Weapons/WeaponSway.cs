using UnityEngine;

namespace Unity.FPS.Gameplay
{
    public class WeaponSway : MonoBehaviour
    {
        [Header("References")]
        public Transform cameraTransform;

        [Header("Sway Settings")]
        public float swayAmount = 0.02f;        // how much positional sway
        public float swayRotAmount = 1.5f;      // how much rotational sway (degrees)
        public float smoothAmount = 8f;         // smoothing speed
        public float maxSwayPos = 0.03f;        // max positional offset
        public float maxSwayRot = 3f;           // max rotational offset in degrees

        Vector3 initialLocalPos;
        Quaternion initialLocalRot;
        Vector3 lastCameraEuler;

        void Start()
        {
            if (cameraTransform == null)
            {
                cameraTransform = Camera.main.transform;
            }

            initialLocalPos = transform.localPosition;
            initialLocalRot = transform.localRotation;
            lastCameraEuler = cameraTransform.eulerAngles;
        }

        void Update()
        {
            ApplySway();
        }

        void ApplySway()
        {
            // Get camera delta rotation (in Euler angles)
            Vector3 currentEuler = cameraTransform.eulerAngles;
            Vector3 deltaEuler = currentEuler - lastCameraEuler;

            // Handle wrap-around (e.g. from 359 to 0)
            deltaEuler.x = Mathf.DeltaAngle(lastCameraEuler.x, currentEuler.x);
            deltaEuler.y = Mathf.DeltaAngle(lastCameraEuler.y, currentEuler.y);
            deltaEuler.z = Mathf.DeltaAngle(lastCameraEuler.z, currentEuler.z);

            lastCameraEuler = currentEuler;

            // Position sway (move slightly opposite to camera movement)
            Vector3 swayPos = new Vector3(
                -deltaEuler.y * swayAmount,
                -deltaEuler.x * swayAmount,
                0
            );
            swayPos = Vector3.ClampMagnitude(swayPos, maxSwayPos);

            // Rotation sway (tilt / roll / pitch)
            Vector3 swayRot = new Vector3(
                deltaEuler.x * swayRotAmount,
                deltaEuler.y * swayRotAmount,
                deltaEuler.y * swayRotAmount * 0.5f
            );
            swayRot.x = Mathf.Clamp(swayRot.x, -maxSwayRot, maxSwayRot);
            swayRot.y = Mathf.Clamp(swayRot.y, -maxSwayRot, maxSwayRot);
            swayRot.z = Mathf.Clamp(swayRot.z, -maxSwayRot, maxSwayRot);

            Quaternion targetRot = Quaternion.Euler(swayRot) * initialLocalRot;

            // Smoothly interpolate local position and rotation
            transform.localPosition = Vector3.Lerp(transform.localPosition, initialLocalPos + swayPos, Time.deltaTime * smoothAmount);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * smoothAmount);
        }
    }
}
