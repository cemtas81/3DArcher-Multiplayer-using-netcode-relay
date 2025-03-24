using Unity.Netcode;
using UnityEngine;


public class Projectile : NetworkBehaviour
{
    private Rigidbody rb;
    private bool isFlying = false;
    public float gravity = -9.8f;
    private Vector3 initialPosition;
    private Vector3 initialVelocity;
    private float timeInFlight = 0f;
    private Collider coll;
    public float damage, arrowSpeed;

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

        timeInFlight += Time.fixedDeltaTime * arrowSpeed;

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
        if (!IsServer) return; // Only the server should handle collision

        isFlying = false;
        rb.isKinematic = true;
        coll.enabled = false;

        if (collision.collider.CompareTag("Player"))
        {
            Debug.Log("Hit");
            IDamagable damagable = collision.collider.GetComponent<IDamagable>();

            if (damagable is NetworkBehaviour networkObject)
            {
                ulong targetId = networkObject.NetworkObjectId;
                ApplyDamageServerRpc(targetId, damage);
            }
        }

        transform.parent = collision.transform;
    }

    [ServerRpc]
    void ApplyDamageServerRpc(ulong targetId, float damage)
    {
        NetworkObject networkObject;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out networkObject))
        {
            IDamagable damagable = networkObject.GetComponent<IDamagable>();

            if (damagable != null)
            {
                Debug.Log($"Applying {damage} damage to {networkObject.name}");
                damagable.Damage(damage); // Call Damage on server
            }
            else
            {
                Debug.LogError($"No IDamagable found on object {networkObject.name}");
            }
        }
        else
        {
            Debug.LogError($"No NetworkObject found with ID {targetId}");
        }
    }

  
}
