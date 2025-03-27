using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class ClientMove : NetworkBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private ThirdPersonController characterController;
    [SerializeField] private StarterAssetsInputs assetsInputs;
    [SerializeField] private TopDownCharacter character;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private float range=8;
    private void Awake()
    {

        playerInput.enabled = false;
        assetsInputs.enabled = false;
        characterController.enabled = false;
        character.enabled = false;
        health.enabled = false;

    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        //if (IsOwner)
        //{
        //    playerInput.enabled = true;
        //    assetsInputs.enabled = true;
        //    //characterController.enabled = true;
        //    character.enabled = true;
        //    health.enabled = true;
        //}
        
        //if(IsServer)
        //{
           
        //    characterController.enabled = true;
        
        //}
        //SpawnServerRpc();
        if (IsHost && IsOwner)
        {
            playerInput.enabled = true;
            assetsInputs.enabled = true;
            characterController.enabled = true;
            character.enabled = true;
            health.enabled = true;
        }
        if (IsServer && IsOwner)
        {
            playerInput.enabled = true;
            assetsInputs.enabled = true;
            characterController.enabled = true;
            character.enabled = true;
            health.enabled = true;
        }
        if (IsClient && IsOwner)
        {
            playerInput.enabled = true;
            assetsInputs.enabled = true;
            characterController.enabled = true;
            character.enabled = true;
            health.enabled = true;
        }
    }
    [Rpc(target:SendTo.Server)]
    private void UpdateInputServerRpc(Vector2 move, Vector2 look, bool jump, bool sprint)
    {
        assetsInputs.move = move;
        assetsInputs.look = look;
        assetsInputs.jump = jump;
        assetsInputs.sprint = sprint;
    }
    [ClientRpc]
    private void UpdateInputClientRpc(Vector2 movey, Vector2 looky, bool jumpy, bool sprinty)
    {
        assetsInputs.move = movey;
        assetsInputs.look = looky;
        assetsInputs.jump = jumpy;
        assetsInputs.sprint = sprinty;
    }
    private void LateUpdate()
    {
        if (!IsOwner) return;

        UpdateInputServerRpc(assetsInputs.move, assetsInputs.look, assetsInputs.jump, assetsInputs.sprint);
        UpdateInputClientRpc(assetsInputs.move, assetsInputs.look, assetsInputs.jump, assetsInputs.sprint);

    }
    [ServerRpc(RequireOwnership =false)]
    private void SpawnServerRpc()
    {
        transform.position = new Vector3(Random.Range(-range, range), range, Random.Range(-range, range));
    }
    
}


