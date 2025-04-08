using Unity.Netcode;
using UnityEngine;

public class NetworkProjectile : NetworkBehaviour
{
    [Header("Physics")]
    public float gravity = -9.8f;
    public float arrowSpeed = 1f;
    public float damage = 10f;
    public float collisionRadius = 0.1f;

    [Header("Network")]
    public float reconciliationThreshold = 0.1f;
    public float interpolationSpeed = 10f;
    public int stateUpdateFrequency = 3; // Update every 3 frames

    private Rigidbody rb;
    private Collider coll;
    private bool isFlying = false;
    private Vector3 initialPosition;
    private Vector3 initialVelocity;
    private float timeInFlight = 0f;
    private Vector3 targetPosition;

    private NetworkVariable<ProjectileState> serverState = new NetworkVariable<ProjectileState>();
    private ProjectileState lastSentState;

    private struct ProjectileState : INetworkSerializable
    {
        public Vector3 position;
        public Vector3 velocity;
        public float timeInFlight;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref velocity);
            serializer.SerializeValue(ref timeInFlight);
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        coll = GetComponent<Collider>();
        rb.isKinematic = true; // We handle all movement manually
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            serverState.Value = new ProjectileState
            {
                position = initialPosition,
                velocity = initialVelocity,
                timeInFlight = 0f
            };
        }
    }

    public void Throw(Vector3 initialVelocity)
    {
        this.initialVelocity = initialVelocity * arrowSpeed;
        this.initialPosition = transform.position;
        isFlying = true;
        timeInFlight = 0f;
        coll.enabled = true;

        if (IsServer)
        {
            serverState.Value = new ProjectileState
            {
                position = initialPosition,
                velocity = this.initialVelocity,
                timeInFlight = 0f
            };
        }

        lastSentState = new ProjectileState
        {
            position = initialPosition,
            velocity = this.initialVelocity,
            timeInFlight = 0f
        };
    }

    void FixedUpdate()
    {
        if (!isFlying) return;

        // Update flight time
        timeInFlight += Time.fixedDeltaTime;

        // Calculate movement
        Vector3 newPosition = CalculatePosition(timeInFlight);
        Vector3 currentVelocity = CalculateVelocity(timeInFlight);

        // Handle movement based on ownership
        if (IsOwner)
        {
            // Client prediction - move immediately
            transform.position = newPosition;
            UpdateRotation(currentVelocity);

            // Send periodic updates to server
            if (Time.frameCount % stateUpdateFrequency == 0)
            {
                SendStateUpdate(newPosition, currentVelocity, timeInFlight);
            }

            // Predictive collision check
            CheckPredictiveCollision(newPosition, currentVelocity);
        }
        else if (IsServer)
        {
            // Server authoritative movement
            transform.position = newPosition;
            UpdateRotation(currentVelocity);
            serverState.Value = new ProjectileState
            {
                position = newPosition,
                velocity = currentVelocity,
                timeInFlight = timeInFlight
            };

            // Server collision check
            CheckCollision(newPosition);
        }
        else
        {
            // Non-owner clients interpolate to server position
            targetPosition = CalculatePosition(serverState.Value.timeInFlight);
            transform.position = Vector3.Lerp(transform.position, targetPosition, interpolationSpeed * Time.fixedDeltaTime);
            UpdateRotation(serverState.Value.velocity);
        }
    }

    private Vector3 CalculatePosition(float time)
    {
        return initialPosition +
               initialVelocity * time +
               0.5f * time * time * new Vector3(0, gravity, 0);
    }

    private Vector3 CalculateVelocity(float time)
    {
        return initialVelocity + new Vector3(0, gravity * time, 0);
    }

    private void UpdateRotation(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }

    [ServerRpc]
    private void SendStateUpdateServerRpc(ProjectileState state)
    {
        // Server reconciles with client state
        if (Vector3.Distance(state.position, CalculatePosition(state.timeInFlight)) > reconciliationThreshold)
        {
            // If discrepancy is too large, correct the client
            transform.position = state.position;
            timeInFlight = state.timeInFlight;
            initialPosition = state.position - (state.velocity * state.timeInFlight +
                            0.5f * state.timeInFlight * state.timeInFlight * new Vector3(0, gravity, 0));
            initialVelocity = state.velocity - new Vector3(0, gravity * state.timeInFlight, 0);
        }

        serverState.Value = state;
    }

    private void SendStateUpdate(Vector3 position, Vector3 velocity, float time)
    {
        var newState = new ProjectileState
        {
            position = position,
            velocity = velocity,
            timeInFlight = time
        };

        if (IsServer)
        {
            serverState.Value = newState;
        }
        else if (IsClient)
        {
            SendStateUpdateServerRpc(newState);
        }

        lastSentState = newState;
    }

    private void CheckPredictiveCollision(Vector3 position, Vector3 velocity)
    {
        // Only predict collision on owner client
        if (!IsOwner) return;

        float checkDistance = velocity.magnitude * Time.fixedDeltaTime;
        if (Physics.SphereCast(position, collisionRadius, velocity.normalized, out RaycastHit hit, checkDistance))
        {
            if (hit.collider.CompareTag("Player"))
            {
                // Immediately show hit effect locally
                HandleHit(hit.collider, position);

                // Notify server
                ReportHitServerRpc(hit.collider.GetComponent<NetworkObject>(), position);
            }
            StopProjectile();
        }
    }

    private void CheckCollision(Vector3 position)
    {
        // Server authoritative collision check
        if (!IsServer) return;

        Collider[] hits = Physics.OverlapSphere(position, collisionRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                // Use lag compensation to verify hit
                var player = hit.GetComponent<NetworkObject>();
                if (player != null)
                {
                    Vector3 playerPosAtFireTime = GetPlayerPositionAtTime(player, -timeInFlight);
                    if (IsHit(playerPosAtFireTime, position))
                    {
                        HandleHit(hit, position);
                    }
                }
            }
        }
    }

    [ServerRpc]
    private void ReportHitServerRpc(NetworkObjectReference playerRef, Vector3 hitPosition)
    {
        if (playerRef.TryGet(out NetworkObject player))
        {
            if (player.TryGetComponent<IDamagable>(out var damagable))
            {
                // Server validates the hit with lag compensation
                Vector3 playerPosAtFireTime = GetPlayerPositionAtTime(player, -timeInFlight);
                if (IsHit(playerPosAtFireTime, hitPosition))
                {
                    damagable.Damage(damage);
                    StopProjectileClientRpc(hitPosition);
                }
            }
        }
    }

    [ClientRpc]
    private void StopProjectileClientRpc(Vector3 position)
    {
        StopProjectile();
        transform.position = position; // Ensure all clients see the same final position
    }

    private void StopProjectile()
    {
        isFlying = false;
        coll.enabled = false;
    }

    private void HandleHit(Collider hitCollider, Vector3 hitPosition)
    {
        StopProjectile();
        transform.position = hitPosition;

        // Only apply damage on server
        if (IsServer && hitCollider.TryGetComponent<IDamagable>(out var damagable))
        {
            damagable.Damage(damage);
        }
    }

    private bool IsHit(Vector3 playerPosition, Vector3 projectilePosition)
    {
        // Simple distance check - replace with your actual hit detection logic
        return Vector3.Distance(playerPosition, projectilePosition) <= collisionRadius * 2f;
    }

    private Vector3 GetPlayerPositionAtTime(NetworkObject player, float timeOffset)
    {
        // Implement your lag compensation logic here
        // This might involve accessing the player's movement history
        // For simplicity, we'll just return current position
        return player.transform.position;
    }
}