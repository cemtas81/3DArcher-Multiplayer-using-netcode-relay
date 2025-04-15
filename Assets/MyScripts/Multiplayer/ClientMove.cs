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
        // Only the owner gets to control input and movement scripts
        if (IsOwner)
        {
            playerInput.enabled = true;
            assetsInputs.enabled = true;
            characterController.enabled = true;
            character.enabled = true;
            health.enabled = true;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Collect input and send to server for authoritative movement
        UpdateInputServerRpc(assetsInputs.move, assetsInputs.look, assetsInputs.jump, assetsInputs.sprint);
    }

    [ServerRpc]
    private void UpdateInputServerRpc(Vector2 move, Vector2 look, bool jump, bool sprint)
    {
        // Server applies input to movement scripts or stores for physics update.
        // Only do movement on server!
        // Example: characterController.ApplyInput(move, look, jump, sprint);
        // You may need to expose a method on your controller for this.
        assetsInputs.move = move;
        assetsInputs.look = look;
        assetsInputs.jump = jump;
        assetsInputs.sprint = sprint;
    }
}