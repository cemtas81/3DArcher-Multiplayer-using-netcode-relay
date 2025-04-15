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
    Vector3 hit;

    private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<Vector3> networkVelocity = new NetworkVariable<Vector3>();

    private Vector3 previousServerPosition;
    private float interpolationTime = 0.1f;
    [SerializeField] GameObject effect;
    private Vector3 clientPredictedPosition;
    private Vector3 clientPredictedVelocity;
    private NetworkObject currentEffect;

    // Add: Sync effect's NetworkObjectId to all clients
    private NetworkVariable<ulong> effectObjectId = new NetworkVariable<ulong>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        coll = GetComponent<Collider>();
    }

    private void OnEffectObjectIdChanged(ulong oldId, ulong newId)
    {
        TryAssignEffect();
    }

    private void TryAssignEffect()
    {
        // Assign effect instance if possible (clients)
        if (IsClient && effectObjectId.Value != 0 && currentEffect == null)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(effectObjectId.Value, out NetworkObject netObj))
            {
                currentEffect = netObj;
            }
        }
    }

    public void Throw(Vector3 initialVelocity)
    {
        if (IsServer)
        {
            coll.enabled = true;
            transform.parent = null;
            rb.isKinematic = false;
            rb.linearVelocity = initialVelocity;
            rb.useGravity = true;
            isFlying = true;

            networkPosition.Value = transform.position;
            networkVelocity.Value = initialVelocity;

            StartCoroutine(DestroyAfterDelay(lifetime));
            // Get a pooled arrow instance from the pool
            currentEffect = NetworkProjectilePool.Singleton.GetNetworkObject(effect, transform.position, transform.rotation);
            currentEffect.Spawn(true);

            // Set NetworkObjectId so clients can assign currentEffect
            effectObjectId.Value = currentEffect.NetworkObjectId;
        }
        else
        {
            coll.enabled = true;
            transform.parent = null;
            rb.isKinematic = false;
            rb.linearVelocity = initialVelocity;
            rb.useGravity = true;
            isFlying = true;

            clientPredictedPosition = transform.position;
            clientPredictedVelocity = initialVelocity;
        }
    }
    void OnEnable()
    {
        if (IsClient)
        {
            networkPosition.OnValueChanged += OnPositionChanged;
            networkVelocity.OnValueChanged += OnVelocityChanged;
        }
        effectObjectId.OnValueChanged += OnEffectObjectIdChanged;
        TryAssignEffect();
    }
    private void OnDisable()
    {
        if (IsClient)
        {
            networkPosition.OnValueChanged -= OnPositionChanged;
            networkVelocity.OnValueChanged -= OnVelocityChanged;
        }
        if (currentEffect != null&&IsServer)
        {
            currentEffect.Despawn();
        }
        effectObjectId.OnValueChanged -= OnEffectObjectIdChanged;
    }
    void FixedUpdate()
    {
        if (!isFlying) return;

        if (IsServer)
        {
            networkPosition.Value = transform.position;
            networkVelocity.Value = rb.linearVelocity;

            if (rb.linearVelocity.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(rb.linearVelocity, Vector3.up);
            }
            if (currentEffect != null)
                currentEffect.transform.SetPositionAndRotation(transform.position, transform.rotation);
        }
        else if (IsClient)
        {
            PredictMovement();
            InterpolateToServerState();
        }
    }
    private void PredictMovement()
    {
        clientPredictedPosition += clientPredictedVelocity * Time.fixedDeltaTime;
        transform.position = clientPredictedPosition;
        if (clientPredictedVelocity.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(clientPredictedVelocity, Vector3.up);
        }
        if (currentEffect != null)
            currentEffect.transform.SetPositionAndRotation(transform.position, transform.rotation);
    }

    private void InterpolateToServerState()
    {
        transform.position = Vector3.Lerp(transform.position, networkPosition.Value, interpolationTime);
        clientPredictedVelocity = Vector3.Lerp(clientPredictedVelocity, networkVelocity.Value, interpolationTime);
        if (currentEffect != null)
            currentEffect.transform.SetPositionAndRotation(transform.position, transform.rotation);
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
        previousServerPosition = newPosition;
    }

    private void OnVelocityChanged(Vector3 oldVelocity, Vector3 newVelocity)
    {
        clientPredictedVelocity = newVelocity;
    }
}