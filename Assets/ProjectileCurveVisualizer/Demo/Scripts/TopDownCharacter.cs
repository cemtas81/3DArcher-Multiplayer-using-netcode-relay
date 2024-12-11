using UnityEngine;
using UnityEngine.UI;
using ProjectileCurveVisualizerSystem;
using UnityEngine.Animations.Rigging;
using Cinemachine;
using UnityEngine.InputSystem;

public class TopDownCharacter : MonoBehaviour
{
    public Transform characterTransform;
    private Transform springArmTransform;
    public Transform cameraTransform;
    public Camera characterCamera;
    public Transform aim;
    private Ray ray;
    private RaycastHit mouseRaycastHit;
    Animator anim;
    private Vector3 targetCharacterPosition;
    public RigBuilder rig;
    public float characterMovementSpeed = 35.0f, camTurnSpeed;
    public float launchSpeed = 15.0f;
    public Transform bow;
    public LayerMask ignoredLayers;
    private CinemachineVirtualCamera cine;
    private Vector3 previousPosition;
    private bool canHitTarget = false;
    public Transform ShootPos;
    public Vector3 updatedProjectileStartPosition;
    public Vector3 projectileLaunchVelocity;
    public Vector3 predictedTargetPosition;
    public RaycastHit hit;
    public Vector3 invertedDragDelta;
    private int gettingHitTimes = 0;
    public Gamepad gamepad;
    public ProjectileCurveVisualizer projectileCurveVisualizer;
    public GameObject projectileGameObject;
    public Text gettingHitTimesText;
    private TopDownGamePadActions tpActions;
    private float buttonPressTime = 0.0f;
    public bool isDragging = false, isAiming;
    private Vector3 initialMousePosition;
    public float maxDragDistance = 5;

    void Start()
    {
        characterTransform = this.transform;
        anim = GetComponent<Animator>();
        springArmTransform = this.transform.GetChild(0).transform;
        springArmTransform.parent = null;
        cameraTransform = springArmTransform.GetChild(0).transform;
        characterCamera = cameraTransform.GetComponent<Camera>();
        cine = cameraTransform.GetComponent<CinemachineVirtualCamera>();
        //characterCamera = Camera.main;
        targetCharacterPosition = characterTransform.position;
        previousPosition = characterTransform.position;
        tpActions = GetComponent<TopDownGamePadActions>();
    }

    void Update()
    {
        gamepad=Gamepad.current;

        CharacterMovementLogic();

        Attributes.characterVelocity = (characterTransform.position - previousPosition) / Time.deltaTime;
        previousPosition = characterTransform.position;
        if (gamepad!=null)
        {
            tpActions.GamePadAction();
        }
        else
        {
            if (Input.GetMouseButton(1))
            {
                buttonPressTime += Time.deltaTime;
                launchSpeed = Mathf.Clamp(15.0f + buttonPressTime * 5.0f, 5.0f, 30.0f);
            }

            if (Input.GetButtonDown("Fire1")) // On mouse button down
            {
                Lock();
            }

            if (Input.GetButton("Fire1")) // While dragging
            {
                Drag();

                if (isDragging)
                {
                    Aim();
                }

            }

            if (Input.GetButtonUp("Fire1")) // On release
            {
                Fire();
            }
        }
       
    }
    private void LateUpdate()
    {
        CameraControlLogic();
        CameraZoomingLogic();
    }
    public void Lock()
    {
        isDragging = false;
        initialMousePosition = Input.mousePosition;
        ray = characterCamera.ScreenPointToRay(initialMousePosition);
        if (Physics.Raycast(ray, out mouseRaycastHit, Mathf.Infinity, ~ignoredLayers))
        {
            characterTransform.LookAt(new Vector3(mouseRaycastHit.point.x, characterTransform.position.y, mouseRaycastHit.point.z));
        }
        anim.SetBool("Aiming", true);

    }
    public void Drag()
    {
        Vector3 currentMousePosition = Input.mousePosition;
        Vector3 dragDelta = currentMousePosition - initialMousePosition;

        isAiming = true;
        rig.enabled = true;
        // Check if drag has started and set `isDragging` to true if moving significantly
        if (dragDelta.magnitude > 10.0f) // Small threshold to detect drag
        {
            // Transform the screen-space drag delta into world space
            Vector3 worldDragDelta = cameraTransform.TransformDirection(new Vector3(dragDelta.x, 0, dragDelta.y));
            // Invert the drag for bow-stretching effect
            invertedDragDelta = new Vector3(-worldDragDelta.x, 0, -worldDragDelta.z);

            // Use the drag only if it opposes the character's forward direction
            if (Vector3.Dot(invertedDragDelta.normalized, characterTransform.forward) > 0)
            {
                isDragging = true;
            }
            else
            {
                isDragging = false;
            }
        }
    }

    public void Fire()
    {
        projectileCurveVisualizer.HideProjectileCurve();
        isDragging = false;
        isAiming = false;
        if (canHitTarget)
        {
            canHitTarget = false;
            Projectile projectile = GameObject.Instantiate(projectileGameObject).GetComponent<Projectile>();
            projectile.transform.SetPositionAndRotation(updatedProjectileStartPosition, Quaternion.LookRotation(projectileLaunchVelocity));
            projectile.Throw(projectileLaunchVelocity);
        }
        anim.SetBool("Aiming", false);
        launchSpeed = 15.0f;
        buttonPressTime = 0.0f;
        rig.enabled = false;
    }
    public void Aim()
    {
        if (!isDragging) return;
        // Rotate the character towards the drag direction
        Vector3 dragDirection = invertedDragDelta.normalized;
        rig.enabled = true;
        Quaternion targetRotation = Quaternion.LookRotation(dragDirection);

        characterTransform.rotation = Quaternion.Slerp(characterTransform.rotation, targetRotation, Time.deltaTime * 10f); // Adjust rotation speed

        float dragDistance = Mathf.Min(invertedDragDelta.magnitude * 0.1f, maxDragDistance);
        Vector3 targetPosition = characterTransform.position + characterTransform.forward * dragDistance;
        aim.transform.position = targetPosition;
        // Visualize the projectile curve with the new target position
        canHitTarget = projectileCurveVisualizer.VisualizeProjectileCurveWithTargetPosition
            (new(ShootPos.position.x, ShootPos.position.y, ShootPos.position.z), 1f, targetPosition, launchSpeed, Vector3.zero, Vector3.zero, 0.05f, 0.1f, false, out updatedProjectileStartPosition, out projectileLaunchVelocity, out predictedTargetPosition, out hit);

        //if (!canHitTarget)
        //{
        //    projectileCurveVisualizer.HideProjectileCurve();
        //    print("Too far, cannot throw to there");
        //}
        if (dragDistance == maxDragDistance)
        {
            buttonPressTime += Time.deltaTime;
            launchSpeed = Mathf.Clamp(15.0f + buttonPressTime * 15.0f, 5.0f, 45.0f);
        }
        else if (dragDistance < maxDragDistance)
        {
            launchSpeed = 15;
        }
    }
    void CameraControlLogic()
    {
        springArmTransform.position = characterTransform.position;
        if (Input.GetKey(KeyCode.Q))
        {
            CamLeft();
        }
        if (Input.GetKey(KeyCode.E))
        {
            CamRight();
        }
    }
    void CamLeft()
    {
        springArmTransform.Rotate(-camTurnSpeed * Time.deltaTime * Vector3.up, Space.World);
    }
    void CamRight()
    {
        springArmTransform.Rotate(camTurnSpeed * Time.deltaTime * Vector3.up, Space.World);
    }
    void CameraZoomingLogic()
    {
        cameraTransform.localPosition = new Vector3(0.0f, 0.0f, Mathf.Clamp(cameraTransform.localPosition.z + Input.GetAxis("Mouse ScrollWheel") * 6.0f, -30.0f, -8.0f));

    }
    void CharacterMovementLogic()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Update animation state
        Animating(horizontalInput, verticalInput);

        // Get the movement direction based on input
        Vector3 movementDirection = new Vector3(horizontalInput, 0, verticalInput).normalized;

        if (movementDirection != Vector3.zero)
        {
            // Transform movement direction relative to the rotating camera
            Vector3 cameraForward = new Vector3(cameraTransform.forward.x, 0, cameraTransform.forward.z).normalized;
            Vector3 cameraRight = new Vector3(cameraTransform.right.x, 0, cameraTransform.right.z).normalized;

            // Adjust movement direction based on camera's orientation
            movementDirection = (cameraForward * movementDirection.z + cameraRight * movementDirection.x).normalized;

            if (!isAiming)
            {
                characterTransform.forward = movementDirection; // Update character's forward direction
            }

            targetCharacterPosition = characterTransform.position + characterMovementSpeed * Time.deltaTime * movementDirection;
        }

        // Smoothly interpolate character's position to the target position
        characterTransform.position = Vector3.Lerp(
            characterTransform.position,
            new Vector3(targetCharacterPosition.x, characterTransform.position.y, targetCharacterPosition.z),
            0.125f
        );
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.name == "Projectile(Clone)")
        {
            gettingHitTimes += 1;
            gettingHitTimesText.text = "Getting hit " + gettingHitTimes + " times";
        }
    }

    void Animating(float h, float v)
    {
        bool walking = h != 0f || v != 0f;
        anim.SetBool("IsWalking", walking);
        Vector3 direction = new(h, 0, v);
        direction = cameraTransform.TransformDirection(direction);
        float velocityZ = Vector3.Dot(direction.normalized, transform.forward * 2);
        float velocityX = Vector3.Dot(direction.normalized, transform.right);
        anim.SetFloat("VelocityX", velocityX);
        anim.SetFloat("VelocityZ", velocityZ);
    }
}
