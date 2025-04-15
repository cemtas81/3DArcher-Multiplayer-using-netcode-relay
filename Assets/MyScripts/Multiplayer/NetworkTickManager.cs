using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Singleton for synchronizing ticks across all clients.
/// </summary>
public class NetworkTickManager : NetworkBehaviour
{
    public static int CurrentTick { get; private set; }
    public static float TickDeltaTime { get; private set; } = 1f / 60f; // 60Hz

    private float tickTimer = 0f;
    [SerializeField] private float tickSyncInterval = 0.2f;
    private float lastSyncTime = 0f;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            CurrentTick = 0;
    }

    void FixedUpdate()
    {
        if (IsServer)
        {
            tickTimer += Time.fixedDeltaTime;
            if (tickTimer >= TickDeltaTime)
            {
                CurrentTick++;
                tickTimer -= TickDeltaTime;
            }

            if (Time.time - lastSyncTime > tickSyncInterval)
            {
                SyncTickClientRpc(CurrentTick);
                lastSyncTime = Time.time;
            }
        }
    }

    [ClientRpc]
    private void SyncTickClientRpc(int serverTick)
    {
        CurrentTick = serverTick;
    }
}