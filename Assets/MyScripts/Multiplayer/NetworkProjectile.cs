using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkProjectile : NetworkBehaviour
{
    private Rigidbody rb;
    private bool isFlying = false;
    public float gravity = -9.8f;
    public float damage, lifetime = 5;
    private float stickDuration = 3f;
    private Collider coll;
    private Vector3 hit;

    // Network variables for syncing position and velocity
    private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<Vector3> networkVelocity = new NetworkVariable<Vector3>();

    private Vector3 previousServerPosition;  // For interpolation
    private float interpolationTime = 0.1f; // Interpolation duration

    private Vector3 clientPredictedPosition; // Client's predicted position
    private Vector3 clientPredictedVelocity; // Client's predicted velocity

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        coll = GetComponent<Collider>();
    }

    public void Throw(Vector3 initialVelocity)
    {
        if (IsServer)
        {
            // Server sets the initial velocity
            coll.enabled = true;
            transform.parent = null;
            rb.isKinematic = false;
            rb.linearVelocity = initialVelocity;
            rb.useGravity = true;
            isFlying = true;

            // Sync initial position and velocity to clients
            networkPosition.Value = transform.position;
            networkVelocity.Value = initialVelocity;

            StartCoroutine(DestroyAfterDelay(lifetime));
        }
        else
        {
            // On clients, simulate immediately using prediction
            clientPredictedPosition = transform.position;
            clientPredictedVelocity = initialVelocity;
            isFlying = true;
        }
    }

    void Start()
    {
        if (IsClient)
        {
            // Listen for changes to position and velocity from the server
            networkPosition.OnValueChanged += OnPositionChanged;
            networkVelocity.OnValueChanged += OnVelocityChanged;
        }
    }

    void FixedUpdate()
    {
        if (!isFlying) return;

        if (IsServer)
        {
            // Server updates the position and syncs it with clients
            networkPosition.Value = transform.position;
            networkVelocity.Value = rb.linearVelocity;

            // Rotate to face the direction of movement
            if (rb.linearVelocity.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(rb.linearVelocity, Vector3.up);
            }
        }
        else if (IsClient)
        {
            // Client performs prediction
            PredictMovement();

            // Interpolate towards the server's authoritative position
            InterpolateToServerState();
        }
    }

    private void PredictMovement()
    {
        // Predict the next position based on velocity
        clientPredictedPosition += clientPredictedVelocity * Time.fixedDeltaTime;

        // Update the transform position immediately
        transform.position = clientPredictedPosition;

        // Rotate to face the predicted direction of movement
        if (clientPredictedVelocity.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(clientPredictedVelocity, Vector3.up);
        }
    }

    private void InterpolateToServerState()
    {
        // Smoothly interpolate position to the server's authoritative position
        transform.position = Vector3.Lerp(transform.position, networkPosition.Value, interpolationTime);

        // Gradually adjust predicted velocity to match the server's velocity
        clientPredictedVelocity = Vector3.Lerp(clientPredictedVelocity, networkVelocity.Value, interpolationTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || !isFlying) return;

        isFlying = false;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        rb.useGravity = false;
        coll.enabled = false;
        hit = collision.contacts[0].point;

        if (collision.collider.CompareTag("Player") && collision.collider.TryGetComponent<IDamagable>(out var damagable))
        {
            ulong targetId = collision.collider.GetComponent<NetworkObject>().NetworkObjectId;
            ApplyDamageServerRpc(targetId, damage, hit);
        }

        StartCoroutine(DestroyAfterDelay(stickDuration));
    }

    [ServerRpc(RequireOwnership = false)]
    void ApplyDamageServerRpc(ulong targetId, float damage, Vector3 pointHit)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out NetworkObject targetObject))
        {
            if (targetObject.TryGetComponent<IDamagable>(out var damagable))
            {
                damagable.Damage(damage, pointHit);
                NotifyDamageClientRpc(targetId, damage);
            }
        }
    }

    [ClientRpc]
    void NotifyDamageClientRpc(ulong targetId, float damage)
    {
        Debug.Log($"Damage applied to {targetId}: {damage}");
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (IsServer)
        {
            var netObj = GetComponent<NetworkObject>();
            netObj.Despawn();
        }
    }

    private void OnPositionChanged(Vector3 oldPosition, Vector3 newPosition)
    {
        // Update server position for interpolation
        previousServerPosition = newPosition;
    }

    private void OnVelocityChanged(Vector3 oldVelocity, Vector3 newVelocity)
    {
        // Update predicted velocity for client-side simulation
        clientPredictedVelocity = newVelocity;
    }
}