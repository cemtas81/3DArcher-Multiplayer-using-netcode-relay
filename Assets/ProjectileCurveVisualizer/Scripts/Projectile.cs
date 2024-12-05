using UnityEngine;

namespace ProjectileCurveVisualizerSystem
{

    public class Projectile : MonoBehaviour
    {
        private Rigidbody rb;
        private bool isFlying = false;
        public float gravity = -9.81f;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        public void Throw(Vector3 initialVelocity)
        {
            rb.linearVelocity = initialVelocity; // Use linearVelocity instead of velocity
            rb.useGravity = false; // Manually apply gravity for precise control
            isFlying = true;
        }

        void FixedUpdate()
        {
            if (!isFlying) return;

            // Apply gravity to the linear velocity
            rb.linearVelocity += gravity * Time.fixedDeltaTime * Vector3.up;

            // Align the arrow's rotation with its velocity vector
            if (rb.linearVelocity.sqrMagnitude > 0.01f) // Avoid jitter at low speeds
            {
                // Use the linear velocity vector to orient the arrow
                transform.rotation = Quaternion.LookRotation(rb.linearVelocity, Vector3.up);
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            rb.linearVelocity = Vector3.zero;
            // Stop the arrow upon impact
            isFlying = false;

            rb.isKinematic = true;

            // Optionally parent the arrow to the object it hits
            transform.parent = collision.transform;
        }
    }


}