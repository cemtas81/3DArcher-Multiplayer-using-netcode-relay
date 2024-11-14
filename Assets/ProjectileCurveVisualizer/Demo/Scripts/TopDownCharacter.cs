using UnityEngine;
using UnityEngine.UI;
using ProjectileCurveVisualizerSystem;

public class TopDownCharacter : MonoBehaviour
{
    private Transform characterTransform;
    private Transform springArmTransform;
    private Transform cameraTransform;
    private Camera characterCamera;
    public Transform aim;
    private Ray ray;
    private RaycastHit mouseRaycastHit;

    private Vector3 targetCharacterPosition;

    public float characterMovementSpeed = 35.0f;
    public float launchSpeed = 15.0f;

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
    private bool isDragging = false;
    private Vector3 initialMousePosition;

    void Start()
    {
        characterTransform = this.transform;

        springArmTransform = this.transform.GetChild(0).transform;
        springArmTransform.parent = null;
        cameraTransform = springArmTransform.GetChild(0).transform;
        characterCamera = cameraTransform.GetComponent<Camera>();

        targetCharacterPosition = characterTransform.position;
        previousPosition = characterTransform.position;
    }
    void Update()
    {
        CameraControlLogic();
        CameraZoomingLogic();
        CharacterMovementLogic();

        Attributes.characterVelocity = (characterTransform.position - previousPosition) / Time.deltaTime;
        previousPosition = characterTransform.position;

        if (Input.GetMouseButton(1))
        {
            buttonPressTime += Time.deltaTime;
            launchSpeed = Mathf.Clamp(15.0f + buttonPressTime * 3.0f, 5.0f, 30.0f);
        }

        if (Input.GetMouseButtonDown(0)) // On mouse button down
        {
            isDragging = false;
            initialMousePosition = Input.mousePosition;
            ray = characterCamera.ScreenPointToRay(initialMousePosition);
            if (Physics.Raycast(ray, out mouseRaycastHit, Mathf.Infinity, ~ignoredLayers))
            {
                characterTransform.LookAt(new Vector3(mouseRaycastHit.point.x, characterTransform.position.y, mouseRaycastHit.point.z));
            }
        }

        if (Input.GetMouseButton(0)) // While dragging
        {
            Vector3 currentMousePosition = Input.mousePosition;
            Vector3 dragDelta = currentMousePosition - initialMousePosition;
          
            // Check if drag has started and set `isDragging` to true if moving significantly
            if (dragDelta.magnitude > 10.0f) // Small threshold to detect drag
            {
                // Calculate inverted drag delta to simulate bow stretch effect
                invertedDragDelta = new(-dragDelta.x, -dragDelta.y, dragDelta.z);

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

            if (isDragging)
            {
                float dragDistance = invertedDragDelta.magnitude * 0.07f; // Adjust the multiplier for sensitivity
                Vector3 targetPosition = characterTransform.position + characterTransform.forward * dragDistance;

                // Visualize the projectile curve with the new target position
                canHitTarget = projectileCurveVisualizer.VisualizeProjectileCurveWithTargetPosition
                    (characterTransform.position, 1f, targetPosition, launchSpeed, Vector3.zero, Vector3.zero, 0.05f, 0.1f, false, out updatedProjectileStartPosition, out projectileLaunchVelocity, out predictedTargetPosition, out hit);

                if (!canHitTarget)
                {
                    projectileCurveVisualizer.HideProjectileCurve();
                    print("Too far, cannot throw to there");
                   
                }
             
            }
         
        }

        if (Input.GetMouseButtonUp(0)) // On release
        {
            projectileCurveVisualizer.HideProjectileCurve();
            isDragging = false;

            if (canHitTarget)
            {
                canHitTarget = false;
                Projectile projectile = GameObject.Instantiate(projectileGameObject).GetComponent<Projectile>();
                projectile.transform.position = updatedProjectileStartPosition;
                projectile.Throw(projectileLaunchVelocity);
            }

            launchSpeed = 15.0f;
            buttonPressTime = 0.0f;
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

        Vector3 movementDirection = new Vector3(horizontalInput, 0, verticalInput).normalized;
       
        if (movementDirection != Vector3.zero)
        {
            //characterTransform.forward = movementDirection;
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
}
