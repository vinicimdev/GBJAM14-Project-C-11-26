using System.Collections;
using UnityEngine;

/// <summary>
/// Smooth camera follow script.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField, Tooltip("")]
    private Transform target;
    [SerializeField, Tooltip("")]
    private Vector3 targetOffset = new(0f, 1f, 0f);

    [Header("Camera Settings")]
    [SerializeField, Tooltip("")]
    private Vector3 cameraOffset = new(-10f, 12f, -10f);

    [Header("Smoothing")]
    [SerializeField, Tooltip(""), Range(0.01f, 1f)]
    private float smoothTime = 0.2f;
    [SerializeField, Tooltip("")]
    private float maxSpeed = 100f;

    [Header("Isometric Settings")]
    [SerializeField, Tooltip("")]
    private bool applyIsometricRotation = true;
    [SerializeField, Tooltip("")]
    private Vector3 isometricEuler = new(30f, 45f, 0f);

    [Header("Zoom")]
    [SerializeField, Tooltip(""), Range(0.05f, 2f)]
    private float zoomTransitionTime = 0.6f;


    private Vector3 _velocity;
    private float _offsetMultiplier = 1f;
    private float _targetMultiplier = 1f;
    private float _multiplierVelocity;

    private void Start()
    {
        if (applyIsometricRotation == true)
        {
            transform.rotation = Quaternion.Euler(isometricEuler);
        }

        if (target != null)
        {
            transform.position = GetDesiredPosition();
            _velocity = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        _offsetMultiplier = Mathf.SmoothDamp(_offsetMultiplier, _targetMultiplier, ref _multiplierVelocity, zoomTransitionTime);

        Vector3 desired = GetDesiredPosition();
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime, maxSpeed);
    }

    private Vector3 GetDesiredPosition()
    {
        return target.position + targetOffset + cameraOffset * _offsetMultiplier;
    }

    /// <summary>
    /// Switches the current camera target.
    /// </summary>
    /// <param name="newTarget">New target for the camera to follow.</param>
    /// <param name="snap">If true, the camera teleports instantly, if not, the camera transitions smoothly.</param>
    public void SetTarget(Transform newTarget, bool snap = false)
    {
        target = newTarget;

        if (snap == true && target != null)
        {
            transform.position = GetDesiredPosition();
            _velocity = Vector3.zero;
        }
    }

    public void SetOffsetMultiplier(float multiplier)
    {
        _targetMultiplier = multiplier;
    }

    /// <summary>
    /// Getter for the current target.
    /// </summary>
    public Transform CurrentTarget => target;
}
