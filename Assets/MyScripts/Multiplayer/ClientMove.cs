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
    [SerializeField] private IsometricCharacterController topDownMover;
    [SerializeField] private PlayerHealth health;

    // Network variables for input
    private NetworkVariable<Vector2> networkMoveInput = new NetworkVariable<Vector2>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private NetworkVariable<Vector2> networkLookInput = new NetworkVariable<Vector2>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private NetworkVariable<bool> networkJumpInput = new NetworkVariable<bool>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private NetworkVariable<bool> networkSprintInput = new NetworkVariable<bool>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private void Awake()
    {
        playerInput.enabled = false;
        assetsInputs.enabled = false;
        characterController.enabled = false;
        if (topDownMover != null)
        {
            topDownMover.enabled = false;
        }
        else
        {
            character.enabled = false;
        }
       
        health.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            playerInput.enabled = true;
            assetsInputs.enabled = true;
            health.enabled = true;

            // Hem server/host hem de client için enable et
            characterController.enabled = true;
            if (topDownMover != null)
            {
                topDownMover.enabled = true;
            }
            else
            {
                character.enabled = true;
            }
        }
        else
        {
            // Disable input processing for non-owned characters
            assetsInputs.enabled = false;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Update network variables with current input
        networkMoveInput.Value = assetsInputs.move;
        networkLookInput.Value = assetsInputs.look;
        networkJumpInput.Value = assetsInputs.jump;
        networkSprintInput.Value = assetsInputs.sprint;
    }

    private void FixedUpdate()
    {
        // Only server processes movement
        if (!IsServer) return;

        // Apply the networked input to the movement system
        assetsInputs.move = networkMoveInput.Value;
        assetsInputs.look = networkLookInput.Value;
        assetsInputs.jump = networkJumpInput.Value;
        assetsInputs.sprint = networkSprintInput.Value;
    }
}