using Unity.Netcode;
using UnityEngine;

public class NetworkCharacterSelection : NetworkBehaviour
{
    public static NetworkCharacterSelection Instance { get; private set; }

    public NetworkList<int> selections = new NetworkList<int>();
    public bool isInitialized = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            selections.Clear();
            foreach (var client in NetworkManager.Singleton.ConnectedClientsIds)
                selections.Add(0); // Default character index
        }

        isInitialized = true;
        Debug.Log("NetworkCharacterSelection initialized");
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetCharacterSelectionServerRpc(int characterIndex, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        ulong clientId = rpcParams.Receive.SenderClientId;
        int index = GetClientIdIndex(clientId);

       
            selections[index] = characterIndex;
            Debug.Log($"Server updated selection for client {clientId} to {characterIndex}");       
        
    }

    public bool TryGetSelectionForClientId(ulong clientId, out int selection)
    {
        selection = 0;
        if (!isInitialized) return false;

        int index = GetClientIdIndex(clientId);
        if (index >= 0 && index < selections.Count)
        {
            selection = selections[index];
            return true;
        }
        return false;
    }

    private int GetClientIdIndex(ulong clientId)
    {
        var ids = NetworkManager.Singleton.ConnectedClientsIds;
        for (int i = 0; i < ids.Count; i++)
        {
            if (ids[i] == clientId)
                return i;
        }
        return -1;
    }
    public int GetSelectionForClientId(ulong clientId)
    {
        int index = GetClientIdIndex(clientId);
        
        if (index >= 0 && index < selections.Count)
            return selections[index];

        Debug.LogWarning($"No selection found for client {clientId}, returning default");
        return 0; // Default
    }
    public override void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        base.OnDestroy();
    }
}