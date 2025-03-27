using System.Collections;
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
    public float damage;
    private float stickDuration = 3f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        coll = GetComponent<Collider>();
    }

    public void Throw(Vector3 initialVelocity)
    {
        if (!IsServer) return;  // Only the server should handle projectile movement.

        this.initialVelocity = initialVelocity;
        this.initialPosition = transform.position;
        rb.useGravity = false;  // Disable gravity for now
        isFlying = true;
        timeInFlight = 0f;
    }

    void FixedUpdate()
    {
        if (!isFlying) return;

        timeInFlight += Time.fixedDeltaTime;

        // Only the server should update the position and velocity
        if (IsServer)
        {
            Vector3 newPosition = initialPosition + initialVelocity * timeInFlight + 0.5f * timeInFlight * timeInFlight * new Vector3(0, gravity, 0);
            transform.position = newPosition;

            // Update rotation based on velocity
            Vector3 currentVelocity = initialVelocity + new Vector3(0, gravity * timeInFlight, 0);
            if (currentVelocity.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(currentVelocity, Vector3.up);
            }
        }
        else
        {
            // Clients receive position updates from the server
            return;  // We don't do physics calculations on clients
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || !isFlying) return; // Only the server should handle collision logic

        if (collision.collider.CompareTag("Player"))
        {
            isFlying = false;
            rb.isKinematic = true;
            coll.enabled = false;
            transform.parent = collision.collider.transform;

            if (collision.collider.TryGetComponent<IDamagable>(out var damagable))
            {
                ulong targetId = collision.collider.GetComponent<NetworkObject>().NetworkObjectId;
                ApplyDamageServerRpc(targetId, damage);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void ApplyDamageServerRpc(ulong targetId, float damage)
    {
        // Apply damage logic on the server
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out NetworkObject targetObject))
        {
            if (targetObject.TryGetComponent<IDamagable>(out var damagable))
            {
                damagable.Damage(damage);  // Apply damage
                NotifyDamageClientRpc(targetId, damage);  // Notify clients about the damage
                DestroyAfterDelayServerRpc(stickDuration);
            }
        }
    }

    [ClientRpc]
    void NotifyDamageClientRpc(ulong targetId, float damage)
    {
        // This is called on all clients to show that damage occurred
        Debug.Log($"Damage applied to {targetId}: {damage}");
    }

    [ServerRpc]
    void DestroyAfterDelayServerRpc(float delay)
    {
        StartCoroutine(DestroyAfterDelay(delay));
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (IsServer)
        {
            GetComponent<NetworkObject>().Despawn(true);  // Despawn the projectile after the delay
        }
    }
}
