using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [Header("Player Prefabs & Spawn Points")]
    [SerializeField] private NetworkObject[] playerPrefabs; // Multiple player prefabs
    [SerializeField] private Transform[] spawnPoints;      // Assign spawn points in Inspector

    private Dictionary<NetworkObject, Transform> prefabSpawnPointMap;
    private int nextSpawnIndex = 0;

    private void Awake()
    {
        if (playerPrefabs.Length != spawnPoints.Length)
        {
            Debug.LogError("The number of player prefabs must match the number of spawn points.");
            return;
        }

        prefabSpawnPointMap = new Dictionary<NetworkObject, Transform>();
        for (int i = 0; i < playerPrefabs.Length; i++)
        {
            prefabSpawnPointMap.Add(playerPrefabs[i], spawnPoints[i]);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points assigned.");
            return;
        }

        // Call the server RPC to spawn the player.
        SpawnPlayerServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerServerRpc(ulong playerId)
    {
        if (nextSpawnIndex >= playerPrefabs.Length)
        {
            Debug.LogError("Not enough spawn points for all players.");
            return;
        }

        var prefab = playerPrefabs[nextSpawnIndex];
        var spawnPoint = prefabSpawnPointMap[prefab];
        var spawn = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        spawn.SpawnAsPlayerObject(playerId);
        nextSpawnIndex++;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        _ = MatchmakingService.LeaveLobby();
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();
    }
}
