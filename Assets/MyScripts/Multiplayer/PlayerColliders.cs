using Unity.Netcode;
using UnityEngine;

public class PlayerColliders : NetworkBehaviour
{
    public PlayerHealth enemyHealth;
    public float damage = 10;
    public float force = 10;


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("EnemyArrow"))
        {

            // Get the NetworkObject
            if (collision.collider.TryGetComponent<NetworkObject>(out var arrowNetObj))
            {


                // Client calls server to handle sticking
                StickArrowServerRpc(arrowNetObj.NetworkObjectId);

            }

            // Apply damage with correct force direction (use arrow's forward direction)
            Vector3 hitPosition = collision.contacts[0].point;
            Vector3 forceDirection = collision.collider.transform.forward;
            //enemyHealth.Damaged(damage, forceDirection * force, hitPosition);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void StickArrowServerRpc(ulong networkObjectId)
    {
       
        StickArrowClientRpc(networkObjectId);
    }
    [ClientRpc(RequireOwnership =false)]
    void StickArrowClientRpc(ulong networkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var arrowObject))
        {
            // Disable physics
            if (arrowObject.TryGetComponent<Rigidbody>(out var arrowRb))
            {
                arrowRb.isKinematic = true;
                arrowRb.linearVelocity = Vector3.zero;
            }
            // Make it stick
            arrowObject.transform.parent = transform;
        }
    }
}