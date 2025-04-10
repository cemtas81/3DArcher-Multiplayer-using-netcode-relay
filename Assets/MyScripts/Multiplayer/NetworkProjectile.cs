using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkProjectile : NetworkBehaviour
{
    private Rigidbody rb;  // Use Rigidbody instead of NetworkRigidbody for manual velocity control
    private bool isFlying = false;
    public float gravity = -9.8f;
    public float damage, lifetime=5;
    private float stickDuration = 3f;
    private Collider coll;
    Vector3 hit;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        coll = GetComponent<Collider>();
    }
    
    public void Throw(Vector3 initialVelocity)
    {
        if (!IsServer) return;  // Only the server should handle projectile movement.

        coll.enabled = true;
        transform.parent = null; // Unstick from the hit object
        rb.isKinematic = false;   // Enable Rigidbody physics
        rb.linearVelocity = initialVelocity;  // Apply initial velocity
        rb.useGravity = true;     // Enable gravity
        isFlying = true;
        StartCoroutine(DestroyAfterDelay(lifetime));
    }
   
    void FixedUpdate()
    {
        if (!isFlying) return;

        // Only the server should update the physics state
        if (IsServer)
        {
            // Gravity is already handled by Unity's physics engine
            if (rb.linearVelocity.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(rb.linearVelocity, Vector3.up);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || !isFlying) return; // Only the server should handle collision logic

        isFlying = false;
        rb.linearVelocity = Vector3.zero;  // Stop movement
        rb.isKinematic = true;  // Stop physics simulation     
        rb.useGravity = false;  // Disable gravity
        coll.enabled = false;
        hit = collision.contacts[0].point; // Get the hit point
        // Stick to the hit object
        //transform.parent = collision.collider.transform;

        if (collision.collider.CompareTag("Player") && collision.collider.TryGetComponent<IDamagable>(out var damagable))
        {
            ulong targetId = collision.collider.GetComponent<NetworkObject>().NetworkObjectId;
            ApplyDamageServerRpc(targetId, damage,hit);
        }

        // Destroy the projectile after a delay
        StartCoroutine(DestroyAfterDelay(stickDuration));
    }

    [ServerRpc(RequireOwnership = false)]
    void ApplyDamageServerRpc(ulong targetId, float damage,Vector3 pointHit)
    {
        // Apply damage logic on the server
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out NetworkObject targetObject))
        {
            if (targetObject.TryGetComponent<IDamagable>(out var damagable))
            {
                damagable.Damage(damage, pointHit);  // Apply damage
                NotifyDamageClientRpc(targetId, damage);  // Notify clients about the damage
            }
        }
    }

    [ClientRpc]
    void NotifyDamageClientRpc(ulong targetId, float damage)
    {
        // This is called on all clients to show that damage occurred
        Debug.Log($"Damage applied to {targetId}: {damage}");
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (IsServer)
        {
            var netObj = GetComponent<NetworkObject>();
            //transform.parent = null; // Unstick from the hit object
            //NetworkProjectilePool.Singleton.ReturnNetworkObject(netObj, originalPrefab); // Return using original prefab reference
            netObj.Despawn(); // Just unspawn
           
        }
    }

}