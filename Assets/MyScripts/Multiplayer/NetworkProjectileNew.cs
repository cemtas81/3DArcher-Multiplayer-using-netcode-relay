using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkProjectileNew : NetworkBehaviour
{
    [SerializeField] private float gravity = -9.8f;
    [SerializeField] private float damage;
    [SerializeField] private float stickDuration = 3f;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private Transform visualTransform; // The visual representation of the projectile

    private Rigidbody rb;
    private Collider coll;
    private bool isFlying = false;
    private List<GameObject> hitTargets = new List<GameObject>();

    // Position interpolation for smooth client-side visuals
    private PositionLerper positionLerper;
    private const float k_LerpTime = 0.1f;
    Vector3 hit;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        coll = GetComponent<Collider>();

        // Ensure we have a visual transform to separate physics from rendering
        if (visualTransform == null)
        {
            visualTransform = transform.Find("Visual");
            if (visualTransform == null)
            {
                // Create a visual object if it doesn't exist
                visualTransform = new GameObject("Visual").transform;
                visualTransform.SetParent(transform);
                visualTransform.localPosition = Vector3.zero;
                visualTransform.localRotation = Quaternion.identity;

                // Move any visual components to this transform
                if (GetComponentInChildren<MeshRenderer>())
                {
                    GetComponentInChildren<MeshRenderer>().transform.SetParent(visualTransform);
                }
            }
        }

        // Ensure we have a trail renderer
        if (trailRenderer == null)
        {
            trailRenderer = GetComponentInChildren<TrailRenderer>();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsClient)
        {
            // Detach visual from parent for smooth interpolation
            visualTransform.parent = null;

            // Clear any existing trail
            if (trailRenderer != null)
            {
                trailRenderer.Clear();
            }

            // Initialize position interpolation
            positionLerper = new PositionLerper(transform.position, k_LerpTime);
            visualTransform.rotation = transform.rotation;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsClient)
        {
            // Clean up trail
            if (trailRenderer != null)
            {
                trailRenderer.Clear();
            }

            // Re-parent visual to keep hierarchy clean
            visualTransform.parent = transform;
        }
    }

    public void Throw(Vector3 initialVelocity)
    {
        if (!IsServer) return; // Only server initiates throwing

        coll.enabled = true;
        transform.parent = null; // Unstick from the hit object
        // Configure physics on server
        rb.isKinematic = false;
        rb.linearVelocity = initialVelocity;
        rb.useGravity = true;
        isFlying = true;

        // Let clients know to start visuals
        ProjectileStartClientRpc(initialVelocity);
    }

    [ClientRpc]
    void ProjectileStartClientRpc(Vector3 initialVelocity)
    {
        if (IsServer) return; // Server already handled this in Throw()

        // Client-side prediction for visuals
        isFlying = true;

        // Don't modify physics on clients, only visuals
        if (trailRenderer != null)
        {
            trailRenderer.enabled = true;
        }
    }

    void FixedUpdate()
    {
        if (!isFlying) return;

        // Only server handles physics
        if (IsServer)
        {
            // Apply physics movement
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity, Vector3.up);
        }
    }

    void Update()
    {
        if (IsClient)
        {
            // Smoothly interpolate the visual position on clients
            if (isFlying)
            {
                if (IsHost)
                {
                    // Host needs to smooth the visual position toward the physics position
                    visualTransform.position = positionLerper.LerpPosition(visualTransform.position, transform.position);
                }
                else
                {
                    // Remote clients just follow the NetworkTransform position
                    visualTransform.position = transform.position;
                }

                // Always align rotation with the direction
                if (rb.linearVelocity.sqrMagnitude > 0.01f)
                {
                    visualTransform.rotation = Quaternion.LookRotation(rb.linearVelocity, Vector3.up);
                }
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || !isFlying) return; // Only server handles collision logic

        isFlying = false;
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.useGravity = false;
        coll.enabled = false;
        hit = collision.contacts[0].point; // Get the hit point
        // Stick to the hit object
        transform.parent = collision.collider.transform;

        // Handle damage if we hit a player
        if (collision.collider.CompareTag("Player") &&
            !hitTargets.Contains(collision.collider.gameObject) &&
            collision.collider.TryGetComponent<IDamagable>(out var damagable))
        {
            hitTargets.Add(collision.collider.gameObject);
            ulong targetId = collision.collider.GetComponent<NetworkObject>().NetworkObjectId;
            ApplyDamageServerRpc(targetId, damage);
        }

        // Notify clients about the impact
        HitObjectClientRpc();

        // Destroy after delay
        StartCoroutine(DestroyAfterDelay(stickDuration));
    }

    [ClientRpc]
    void HitObjectClientRpc()
    {
        // Handle client-side impact effects
        isFlying = false;

        if (IsServer) return; // Server already handled this

        // Make visual stick where it is
        visualTransform.parent = transform;

        // Disable trail
        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
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
                damagable.Damage(damage, hit);  // Apply damage
                NotifyDamageClientRpc(targetId, damage);  // Notify clients
            }
        }
    }

    [ClientRpc]
    void NotifyDamageClientRpc(ulong targetId, float damage)
    {
        Debug.Log($"Damage applied to {targetId}: {damage}");
        // Here you could add hit effects, impact sounds, etc.
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (IsServer)
        {
            var netObj = GetComponent<NetworkObject>();
            
            netObj.Despawn(); // Just unspawn
        }
    }
   
}

// Position interpolation helper class
public class PositionLerper
{
    private Vector3 m_TargetPosition;
    private float m_LerpTime;
    private float m_CurrentLerpTime;
    private Vector3 m_StartPosition;

    public PositionLerper(Vector3 startPosition, float lerpTime)
    {
        m_TargetPosition = startPosition;
        m_StartPosition = startPosition;
        m_LerpTime = lerpTime;
    }

    public Vector3 LerpPosition(Vector3 currentPosition, Vector3 targetPosition)
    {
        // If we have a new target position
        if (targetPosition != m_TargetPosition)
        {
            m_StartPosition = currentPosition;
            m_TargetPosition = targetPosition;
            m_CurrentLerpTime = 0;
        }

        // Calculate position
        m_CurrentLerpTime += Time.deltaTime;
        float t = Mathf.Clamp01(m_CurrentLerpTime / m_LerpTime);
        return Vector3.Lerp(m_StartPosition, m_TargetPosition, t);
    }
    
}