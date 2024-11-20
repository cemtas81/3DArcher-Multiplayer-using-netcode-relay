using UnityEngine;
using UnityEngine.UI;
using ProjectileCurveVisualizerSystem;
using UnityEngine.Animations.Rigging;

public class TopDownCharacter : MonoBehaviour
{
    private Transform characterTransform;
    private Transform springArmTransform;
    private Transform cameraTransform;
    private Camera characterCamera;
    public Transform aim;
    private Ray ray;
    private RaycastHit mouseRaycastHit;
    Animator anim;
    private Vector3 targetCharacterPosition;
    public RigBuilder rig;
    public float characterMovementSpeed = 35.0f;
    public float launchSpeed = 15.0f;
    public Transform bow;
    public LayerMask ignoredLayers;

    private Vector3 previousPosition;
    private bool canHitTarget = false;

    private Vector3 updatedProjectileStartPosition;
    private Vector3 projectileLaunchVelocity;
    private Vector3 predictedTargetPosition;
    private RaycastHit hit;
    Vector3 invertedDragDelta;
    private int gettingHitTimes = 0;

    public ProjectileCurveVisualizer projectileCurveVisualizer;
    public GameObject projectileGameObject;
    public Text gettingHitTimesText;

    private float buttonPressTime = 0.0f;
    private bool isDragging = false, isAiming;
    private Vector3 initialMousePosition;

    void Start()
    {
        characterTransform = this.transform;
        anim = GetComponent<Animator>();
        springArmTransform = this.transform.GetChild(0).transform;
        springArmTransform.parent = null;
        cameraTransform = springArmTransform.GetChild(0).transform;
        characterCamera = cameraTransform.GetComponent<Camera>();
       
        targetCharacterPosition = characterTransform.position;
        previousPosition = characterTransform.position;
    }

    void Update()
    {
       
      
        CharacterMovementLogic();

        Attributes.characterVelocity = (characterTransform.position - previousPosition) / Time.deltaTime;
        previousPosition = characterTransform.position;

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
    private void LateUpdate()
    {
        CameraControlLogic();
        CameraZoomingLogic();
    }
    void Lock()
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
    void Drag()
    {
        Vector3 currentMousePosition = Input.mousePosition;
        Vector3 dragDelta = currentMousePosition - initialMousePosition;
        isAiming = true;
        // Check if drag has started and set `isDragging` to true if moving significantly
        if (dragDelta.magnitude > 10.0f) // Small threshold to detect drag
        {
            // Calculate inverted drag delta to simulate bow stretch effect
            invertedDragDelta = new(-dragDelta.x, 0, -dragDelta.y);

            // Calculate the character's forward direction in screen space
            Vector3 characterForward = characterTransform.forward;

            // Check if the inverted drag is opposite to the character's forward direction
            if (Vector3.Dot(invertedDragDelta.normalized, characterForward.normalized) > 0)
            {
                isDragging = true;
            }
            else
            {
                isDragging = false;
            }

        }
    }
    void Fire()
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
    void Aim()
    {
        // Rotate the character towards the drag direction
        Vector3 dragDirection = invertedDragDelta.normalized;
        rig.enabled = true;
        Quaternion targetRotation = Quaternion.LookRotation(dragDirection);

        characterTransform.rotation = Quaternion.Slerp(characterTransform.rotation, targetRotation, Time.deltaTime * 10f); // Adjust rotation speed

        float dragDistance = invertedDragDelta.magnitude * 0.08f; // Adjust the multiplier for sensitivity
        Vector3 targetPosition = characterTransform.position + characterTransform.forward * dragDistance;
        aim.transform.position = targetPosition;
        // Visualize the projectile curve with the new target position
        canHitTarget = projectileCurveVisualizer.VisualizeProjectileCurveWithTargetPosition
            (new(characterTransform.position.x,1.3f,characterTransform.position.z), 1f, targetPosition, launchSpeed, Vector3.zero, Vector3.zero, 0.05f, 0.1f, false, out updatedProjectileStartPosition, out projectileLaunchVelocity, out predictedTargetPosition, out hit);

        if (!canHitTarget)
        {
            projectileCurveVisualizer.HideProjectileCurve();
            print("Too far, cannot throw to there");
        }
    }
    void CameraControlLogic()
    {
        springArmTransform.position = characterTransform.position;
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
            if (!isAiming)
            {
                characterTransform.forward = movementDirection;
            }

            targetCharacterPosition = characterTransform.position + characterMovementSpeed * Time.deltaTime * movementDirection;
        }

        characterTransform.position = Vector3.Lerp(characterTransform.position, new Vector3(targetCharacterPosition.x, characterTransform.position.y, targetCharacterPosition.z), 0.125f);
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
        float velocityZ = Vector3.Dot(direction.normalized, transform.forward);
        float velocityX = Vector3.Dot(direction.normalized, transform.right);
        anim.SetFloat("VelocityX", velocityX);
        anim.SetFloat("VelocityZ", velocityZ);
    }
}
