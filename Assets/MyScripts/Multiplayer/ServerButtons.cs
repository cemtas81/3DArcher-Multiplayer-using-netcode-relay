using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ServerButtons : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI networkText;
    [SerializeField] private NetworkVariable<int> playersIn=new NetworkVariable<int>(0,NetworkVariableReadPermission.Everyone);
    private void Update()
    {
        networkText.text = "Players in: " + playersIn.Value.ToString();
        if (!IsServer) return;
        playersIn.Value = NetworkManager.Singleton.ConnectedClientsList.Count;
       
    }
    public void StartServer()
    {
        if (!NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.StartServer();
    }
    public void StartHost()
    {
        if (!NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.StartHost();
    }
    public void StartClient()
    {
        if (!NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.StartClient();
    }
}
