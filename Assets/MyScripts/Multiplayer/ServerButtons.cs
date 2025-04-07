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
        NetworkManager.Singleton.StartServer();
    }
    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
    }
    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }
}
