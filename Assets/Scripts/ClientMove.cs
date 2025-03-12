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
        if (IsOwner)
        {
            playerInput.enabled = true;
            assetsInputs.enabled = true;
            characterController.enabled = true;
            character.enabled = true;
            health.enabled = true;
        }
        if (IsServer)
        {

            characterController.enabled = true;

        }
    }
    [ServerRpc]
    private void UpdateInputServerRpc(Vector2 move, Vector2 look, bool jump, bool sprint)
    {
        assetsInputs.move = move;
        assetsInputs.look = look;
        assetsInputs.jump = jump;
        assetsInputs.sprint = sprint;
    }
    private void LateUpdate()
    {
        if (!IsOwner) return;

        UpdateInputServerRpc(assetsInputs.move, assetsInputs.look, assetsInputs.jump, assetsInputs.sprint);
    }
}


