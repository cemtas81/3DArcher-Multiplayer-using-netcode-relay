using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    private Rigidbody rb;
    private bool isFlying = false,Hitted=false;
    public float gravity = -9.8f;
    private Vector3 initialPosition;
    private Vector3 initialVelocity;
    private float timeInFlight = 0f;
    private Collider coll;
    public float damage, arrowSpeed;
    private IDamagable damagable;
    private float stickDuration=3;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        coll = GetComponent<Collider>();
        if (!IsServer) rb.isKinematic = true; // Clients should not simulate physics
    }

    public void Throw(Vector3 initialVelocity)
    {
        this.initialVelocity = initialVelocity;
        this.initialPosition = transform.position;
        rb.useGravity = false;
        isFlying = true;
        timeInFlight = 0f;
    }

    void FixedUpdate()
    {
        //if (!IsOwner) return;
        if (!isFlying) return;

        timeInFlight += Time.fixedDeltaTime * arrowSpeed;

        Vector3 newPosition = initialPosition +
                            initialVelocity * timeInFlight +
                            .5f * timeInFlight * timeInFlight * new Vector3(0, gravity, 0);

        Vector3 currentVelocity = initialVelocity + new Vector3(0, gravity * timeInFlight, 0);

        transform.position = newPosition;

        if (currentVelocity.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(currentVelocity, Vector3.up);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!IsServer||!isFlying) return; // Only the server should handle collision logic

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

    [ServerRpc]
    void ApplyDamageServerRpc(ulong targetId, float damage)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out NetworkObject targetObject))
        {
            if (targetObject.TryGetComponent<IDamagable>(out var damagable))
            {
                damagable.Damage(damage);
                NotifyDamageClientRpc(targetId, damage);
                DestroyAfterDelayServerRpc(stickDuration);
            }
        }
    }

    [ClientRpc]
    void NotifyDamageClientRpc(ulong targetId, float damage)
    {
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
            GetComponent<NetworkObject>().Despawn(true);
        }
    }


}