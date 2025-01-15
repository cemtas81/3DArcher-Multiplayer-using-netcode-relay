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
    Vector3 movement;
    private float buttonPressTime = 0.0f;
    public bool isDragging = false, isAiming;
    private Vector3 initialMousePosition;
    public float maxDragDistance = 5,speed=5;
    private bool isGamepadAiming = false;
    private Vector2 gamepadAimDirection;
    MyInput myInput;
    InputAction aiming;InputAction moving;
    Rigidbody playerRigidbody;
    private Vector3 currentAimDirection = Vector3.zero;
    public float horizontalAimSmoothSpeed = 8f;
    private float currentDrawStrength = 15f;
    private float drawSmoothSpeed = 5f;
    private const float MIN_DRAW_THRESHOLD = 0.1f;
    private const float MAX_DRAW_THRESHOLD = 0.95f;
    Vector2 aimDirection;
    private void Awake()
    {
        myInput = new MyInput();
    }
    private void OnEnable()
    {
        myInput.Enable();
        aiming = myInput.TopDown.Aim;
        moving = myInput.TopDown.Move;
    }
    private void OnDisable()
    {
        myInput.Disable();
    }
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
        playerRigidbody = GetComponent<Rigidbody>();

    }

    void Update()
    {
        gamepad = Gamepad.current;
        aimDirection = aiming.ReadValue<Vector2>();
        CharacterMovementLogic();

        previousPosition = characterTransform.position;

        if (gamepad != null) HandleGamepadAiming();
        else HandleMouseAiming();
    }
    private void HandleMouseAiming()
    {
        if (Input.GetMouseButton(1))
        {
            buttonPressTime += Time.deltaTime;
            launchSpeed = Mathf.Clamp(15.0f + buttonPressTime * 5.0f, 5.0f, 30.0f);
        }

        if (Input.GetButtonDown("Fire1")) 
        {
            Lock();
        }

        if (Input.GetButton("Fire1")) 
        {
            Drag();

            if (isDragging)
            {
                Aim();
            }
            
        }

        if (Input.GetButtonUp("Fire1")) 
        {
            Fire2();
        }
        if (!isDragging)
        {
            MouseTurn();
        }
    }
    void DragWithGamepad()
    {
        isDragging = true;
        rig.enabled = true;
          
        // Get trigger value (ranges from 0 to 1)
        float triggerValue = gamepad.leftTrigger.ReadValue();

        // Smooth the draw strength based on trigger pressure
        currentDrawStrength = Mathf.Lerp(currentDrawStrength, triggerValue, Time.deltaTime * drawSmoothSpeed);

        // Handle horizontal aiming with right stick - just use direction
        if (gamepadAimDirection.magnitude > 0.1f)
        {
            // Convert input to aim direction
            Vector3 targetAimDirection = new (gamepadAimDirection.x , 0, gamepadAimDirection.y );

            // Smooth the rotation
            currentAimDirection = Vector3.Lerp(currentAimDirection, targetAimDirection, Time.deltaTime * horizontalAimSmoothSpeed);

            // Rotate character
            Quaternion targetRotation = Quaternion.LookRotation(currentAimDirection);
            characterTransform.rotation = Quaternion.Slerp(
                characterTransform.rotation,
                targetRotation,
                Time.deltaTime * horizontalAimSmoothSpeed
            );
        }

        // Handle vertical aiming with trigger
        if (currentDrawStrength > MIN_DRAW_THRESHOLD)
        {
            // Calculate aim position based on draw strength
            float drawDistance = Mathf.Lerp(0, maxDragDistance, currentDrawStrength);
            Vector3 targetPosition = characterTransform.position + characterTransform.forward * drawDistance;

            // Update aim position
            aim.transform.position = Vector3.Lerp(
                aim.transform.position,
                targetPosition,
                Time.deltaTime * drawSmoothSpeed
            );

            if (currentDrawStrength >= MAX_DRAW_THRESHOLD)
            {
                buttonPressTime += Time.deltaTime;
                launchSpeed = Mathf.Clamp(15.0f + buttonPressTime * 25.0f, 15.0f, 45);
            }
            else
            {
                buttonPressTime = 0.0f;
            }

            // Visualize projectile curve
            canHitTarget = projectileCurveVisualizer.VisualizeProjectileCurveWithTargetPosition(
                ShootPos.position,
                1f,
                targetPosition,
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
        else
        {
            projectileCurveVisualizer.HideProjectileCurve();
            launchSpeed = 15.0f;
            buttonPressTime = 0.0f;
        }
    }

    void HandleGamepadAiming()
    {
        gamepadAimDirection = aimDirection;
        if (!isAiming) Turning();
        if (gamepad.leftTrigger.ReadValue() > MIN_DRAW_THRESHOLD)
        {
            if (!isGamepadAiming)
            {
                Lock();
                isGamepadAiming = true;
            }
            DragWithGamepad();
        }
        else if (isGamepadAiming)
        {
            Fire();
            isGamepadAiming = false;
            currentDrawStrength = 0f;
        }
    }

    public void Fire()
    {
        gamepad?.SetMotorSpeeds(0, 0); // Stop haptic feedback if you're using it

        projectileCurveVisualizer.HideProjectileCurve();
        isDragging = false;
        isAiming = false;
        if (canHitTarget && currentDrawStrength > MIN_DRAW_THRESHOLD)
        {
            canHitTarget = false;
            Projectile projectile = GameObject.Instantiate(projectileGameObject).GetComponent<Projectile>();
            projectile.transform.SetPositionAndRotation(updatedProjectileStartPosition, Quaternion.LookRotation(projectileLaunchVelocity));
            projectile.Throw(projectileLaunchVelocity);
        }
        anim.SetBool("Aiming", false);
        launchSpeed = 15.0f;
        buttonPressTime = 0.0f;
        currentDrawStrength = 0f;
        rig.enabled = false;
       
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
        if (Physics.Raycast(ray, out mouseRaycastHit, Mathf.Infinity, ~ignoredLayers)&&gamepad==null)
        {
            characterTransform.LookAt(new Vector3(mouseRaycastHit.point.x, characterTransform.position.y, mouseRaycastHit.point.z));
        }
        anim.SetBool("Aiming", true);

    }
    private void MouseTurn()
    {
        ray = characterCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out mouseRaycastHit, Mathf.Infinity, ~ignoredLayers))
        {
            characterTransform.LookAt(new Vector3(mouseRaycastHit.point.x, characterTransform.position.y, mouseRaycastHit.point.z));
        }
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

    public void Fire2()
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
        if (isDragging)
        {
            canHitTarget = projectileCurveVisualizer.VisualizeProjectileCurveWithTargetPosition
           (new(ShootPos.position.x, ShootPos.position.y, ShootPos.position.z), 1f, targetPosition, launchSpeed, Vector3.zero, Vector3.zero, 0.05f, 0.1f, false, out updatedProjectileStartPosition, out projectileLaunchVelocity, out predictedTargetPosition, out hit);

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

        Animating(horizontalInput, verticalInput);

        Vector3 movementDirection = new Vector3(horizontalInput, 0, verticalInput).normalized;

        if (movementDirection != Vector3.zero)
        {
           
            movement.Set(horizontalInput, 0, verticalInput);
            movement = cameraTransform.TransformDirection(movement);
            movement.y = 0;
            movement = speed * Time.deltaTime * movement.normalized;
            playerRigidbody.MovePosition(playerRigidbody.position + movement);
        }

    }
  
    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("EnemyArrow"))
        {
          Debug.Log("Hit");
            
        }
    }
    void Turning()
    {
        Quaternion targetRotation = Quaternion.LookRotation(aimDirection);
        targetRotation.y = 0;
        characterTransform.LookAt(characterTransform.position + new Vector3(aimDirection.x, 0, aimDirection.y));     
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
