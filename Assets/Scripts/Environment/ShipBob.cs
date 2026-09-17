using UnityEngine;

public class ShipBob : MonoBehaviour
{
    [SerializeField] float bobHeight = 0.15f;
    [SerializeField] float tiltAngle = 3f;
    [SerializeField] float speed = 1f;

    Vector3 startPosition;
    Quaternion startRotation;

    void Awake()
    {
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    void Update()
    {
        float t = Time.time * speed;
        float heave = Mathf.Sin(t);
        float roll = Mathf.Sin(t * 1.7f + 1.3f);
        float pitch = Mathf.Sin(t * 2.3f + 2.6f);

        transform.localPosition = startPosition + Vector3.up * ((heave + roll + pitch) / 3f * bobHeight);
        transform.localRotation = startRotation * Quaternion.Euler(pitch * tiltAngle, 0f, roll * tiltAngle);
    }
}
