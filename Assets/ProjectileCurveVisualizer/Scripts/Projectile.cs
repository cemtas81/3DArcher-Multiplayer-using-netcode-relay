using UnityEngine;

namespace ProjectileCurveVisualizerSystem
{
    public class Projectile : MonoBehaviour
    {
        private Rigidbody rb;
        private bool isFlying = false;
        public float gravity = -9.8f;
        private Vector3 initialPosition;
        private Vector3 initialVelocity;
        private float timeInFlight = 0f;
        private Collider coll;
        public float damage,arrowSpeed;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            coll = GetComponent<Collider>();
        }

        public void Throw(Vector3 initialVelocity)
        {
            this.initialVelocity = initialVelocity;
            this.initialPosition = transform.position;
            rb.useGravity = false;
            //rb.isKinematic = true; // We'll handle the movement ourselves
            isFlying = true;
            timeInFlight = 0f;
        }

        void FixedUpdate()
        {
            if (!isFlying) return;

            timeInFlight += Time.fixedDeltaTime*arrowSpeed;

            // Calculate the new position using projectile motion equations
            Vector3 newPosition = initialPosition +
                                initialVelocity * timeInFlight +
                                .5f * timeInFlight * timeInFlight * new Vector3(0, gravity, 0);

            // Calculate current velocity for rotation
            Vector3 currentVelocity = initialVelocity + new Vector3(0, gravity * timeInFlight, 0);

            // Update position
            transform.position = newPosition;

            // Update rotation to face the direction of travel
            if (currentVelocity.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(currentVelocity, Vector3.up);
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            isFlying = false;
            rb.isKinematic = true;
            coll.enabled = false;
            // Optionally parent the projectile to the hit object
            transform.parent = collision.transform;
            if (collision.collider.CompareTag("Player"))
            {
                Debug.Log("Hit");
                IDamagable damagable = collision.collider.GetComponent<IDamagable>();
                damagable.Damage(damage);
            }
        }

        // Optional: Add method to check if projectile has hit something or gone too far
        public bool IsActive()
        {
            return isFlying;
        }
    }
}