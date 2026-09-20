using UnityEngine;

[RequireComponent (typeof(Collider))]
public class RampTrigger : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") == false)
        {
            return;
        }

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            return;
        }

        rb.constraints &= ~RigidbodyConstraints.FreezePositionY;
        rb.useGravity = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") == false)
        {
            return;
        }

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            return;
        }

        Vector3 v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;

        rb.constraints |= RigidbodyConstraints.FreezePositionY;
        rb.useGravity = false;
    }
}
