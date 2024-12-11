using UnityEngine;
using UnityEngine.InputSystem;

public class TopDownGamePadActions : TopDownCharacter
{
    MyInput myInput;
    InputAction aiming;
    private void Awake()
    {
        myInput=new MyInput();
    }
    private void OnEnable()
    {
        myInput.Enable();
        aiming=myInput.TopDown.Aim;
    }
    private void OnDisable()
    {
        myInput.Disable();
    }
    public void GamePadAction()
    {
        Vector2 rightStickInput = aiming.ReadValue<Vector2>();
        if (rightStickInput.magnitude > 0.1f) // Threshold to detect input
        {
            AimWithGamepad(rightStickInput);
        }

        if (gamepad.rightTrigger.wasPressedThisFrame) // Start drag
        {
            Lock();
        }

        if (gamepad.rightTrigger.isPressed) // While dragging
        {
            DragWithGamepad(rightStickInput);
        }

        if (gamepad.rightTrigger.wasReleasedThisFrame) // Release to fire
        {
            Fire();
        }
    }
    void AimWithGamepad(Vector2 rightStickInput)
    {
        // Convert right stick input to world-space direction
        Vector3 aimDirection = new Vector3(rightStickInput.x, 0, rightStickInput.y).normalized;

        // Rotate character to face the aiming direction
        if (aimDirection != Vector3.zero)
        {
            characterTransform.forward = aimDirection;
        }

        // Update aim position
        Vector3 aimPosition = characterTransform.position + aimDirection * maxDragDistance;
        aim.position = aimPosition;

        // Visualize the projectile curve
        projectileCurveVisualizer.VisualizeProjectileCurveWithTargetPosition(
            ShootPos.position,
            1f,
            aimPosition,
            launchSpeed,
            Vector3.zero,
            Vector3.zero,
            0.05f,
            0.1f,
            false,
            out updatedProjectileStartPosition,
            out projectileLaunchVelocity,
            out predictedTargetPosition,
            out hit
        );
    }

    // New function for dragging with gamepad
    void DragWithGamepad(Vector2 rightStickInput)
    {
        isDragging = true;

        // Convert right stick input to drag direction
        Vector3 dragDirection = new Vector3(-rightStickInput.x, 0, -rightStickInput.y).normalized;
        invertedDragDelta = dragDirection;

        // Update aim position based on drag
        Vector3 dragPosition = characterTransform.position + dragDirection * maxDragDistance;
        aim.position = dragPosition;

        // Visualize the updated curve
        projectileCurveVisualizer.VisualizeProjectileCurveWithTargetPosition(
            ShootPos.position,
            1f,
            dragPosition,
            launchSpeed,
            Vector3.zero,
            Vector3.zero,
            0.05f,
            0.1f,
            false,
            out updatedProjectileStartPosition,
            out projectileLaunchVelocity,
            out predictedTargetPosition,
            out hit
        );
    }
}
