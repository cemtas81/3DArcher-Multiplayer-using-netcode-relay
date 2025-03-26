using Unity.Netcode;
using UnityEngine;

public class NetworkHealthState : NetworkBehaviour 
{ 
    public NetworkVariable<float> health = new NetworkVariable<float>();
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        health.Value= 100;
    }

}
