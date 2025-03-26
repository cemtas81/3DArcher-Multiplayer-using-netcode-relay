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
        isFlying = true;
        timeInFlight = 0f;
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;
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
        if (!IsOwner) return;

        isFlying = false;
        rb.isKinematic = true;
        coll.enabled = false;

        if (collision.collider.CompareTag("Player"))
        {
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
        ApplyDamage(targetId, damage);
    }

    private void ApplyDamage(ulong targetId, float damage)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out NetworkObject targetObject))
        {
            if (targetObject.TryGetComponent<IDamagable>(out var damagable))
            {
                Debug.Log($"Applying {damage} damage to {targetObject.name}");
                damagable.Damage(damage);
            }
        }
    }
}