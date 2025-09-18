using Cinemachine;
using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class IsometricCharacterController : NetworkBehaviour
{
    [Header("Hareket Ayarlarý")]
    [SerializeField] private float moveSpeed = 5f, scroolSpeed = 16f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Kamera Ayarlarý")]
    [SerializeField] private float camTurnSpeed = 60f;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private CinemachineVirtualCamera vCam;
    [SerializeField] private float minZoomDistance = 14f;
    [SerializeField] private float maxZoomDistance = 34f;

    [Header("Referanslar")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private ArrowLauncher arrowLauncher;
    [SerializeField] private IKControl kControl;

    private ThirdPersonController _thirdPersonController;
    private Animator _animator;
    private Gamepad _gamepad;
    private MyInput myInput;
    private InputAction aiming;
    private Quaternion _targetRotation;
    private RaycastHit mouseRaycastHit;
    [SerializeField] private LayerMask ignoredLayers;
    private bool wasAiming;
    public LayerMask _groundLayers;

    private void Awake()
    {
        myInput = new MyInput();
        _animator = GetComponent<Animator>();
        if (kControl == null) kControl = GetComponent<IKControl>();
        if (mainCamera == null) mainCamera = Camera.main;
        if (_thirdPersonController == null) _thirdPersonController = GetComponent<ThirdPersonController>();
        if (arrowLauncher == null) arrowLauncher = GetComponent<ArrowLauncher>();
    }

    private void OnEnable()
    {
        myInput.Enable();
        aiming = myInput.TopDown.Aim;
    }

    private void OnDisable()
    {
        myInput.Disable();
    }

    private void Start()
    {
        // Kamerayý bul ve direkt olarak ata
        vCam = FindFirstObjectByType<CinemachineVirtualCamera>();
        if (vCam != null)
        {
            vCam.Follow = transform;
            vCam.LookAt = transform;
        }
        
        // Diðer kamera referanslarýný güncelle
        if (mainCamera == null) mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!IsOwner) return;

        CalculateRotation();
        MoveCharacter();

        if (arrowLauncher != null)
        {
            if (wasAiming && !arrowLauncher.isAiming)
                ResetAimingState();
            wasAiming = arrowLauncher.isAiming;
        }
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;
        CameraControlLogic();
        CameraZoomingLogic();
    }

    private void CalculateRotation()
    {
        if (arrowLauncher != null && arrowLauncher.isAiming)
            Lock();
        else
            RotateTowardsMouse();

        transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime * rotationSpeed);
    }

    private void RotateTowardsMouse()
    {
        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out mouseRaycastHit, Mathf.Infinity, ~ignoredLayers))
            _targetRotation = Quaternion.LookRotation(new Vector3(mouseRaycastHit.point.x - transform.position.x, 0, mouseRaycastHit.point.z - transform.position.z));
    }

    private void MoveCharacter()
    {
        _gamepad = Gamepad.current;
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        Animating(horizontalInput, verticalInput);
    }

    private void Animating(float h, float v)
    {
        bool walking = h != 0f || v != 0f;
        _animator.SetBool("IsWalking", walking);

        Vector3 direction = new(h, 0, v);
        direction = mainCamera.transform.TransformDirection(direction);
        float velocityZ = Vector3.Dot(direction.normalized, transform.forward * 2);
        float velocityX = Vector3.Dot(direction.normalized, transform.right);

        _animator.SetFloat("VelocityX", velocityX);
        _animator.SetFloat("VelocityZ", velocityZ);
    }

    private void CameraControlLogic()
    {
        if (_gamepad != null)
        {
            if (_gamepad.dpad.left.IsPressed()) CamLeft();
            if (_gamepad.dpad.right.IsPressed()) CamRight();
        }
        else
        {
            if (Input.GetKey(KeyCode.Q)) CamLeft();
            if (Input.GetKey(KeyCode.E)) CamRight();
        }
    }

    private void CamLeft()
    {
        if (vCam != null)
            vCam.transform.Rotate(-camTurnSpeed * Time.deltaTime * Vector3.up, Space.World);
    }

    private void CamRight()
    {
        if (vCam != null)
            vCam.transform.Rotate(camTurnSpeed * Time.deltaTime * Vector3.up, Space.World);
    }

    private void CameraZoomingLogic()
    {
        if (vCam == null) return;
        var framingTransposer = vCam.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (framingTransposer == null) return;

        float targetDistance = framingTransposer.m_CameraDistance;

        if (_gamepad == null)
        {
            targetDistance -= Input.GetAxis("Mouse ScrollWheel") * scroolSpeed;
        }
        else
        {
            float zoomInput = 0f;
            if (_gamepad.dpad.up.isPressed) zoomInput = -1f;
            else if (_gamepad.dpad.down.isPressed) zoomInput = 1f;
            targetDistance += zoomInput * 0.3f * Time.deltaTime * 60f;
        }

        targetDistance = Mathf.Clamp(targetDistance, minZoomDistance, maxZoomDistance);
        framingTransposer.m_CameraDistance = Mathf.Lerp(
            framingTransposer.m_CameraDistance,
            targetDistance,
            Time.deltaTime * smoothSpeed
        );
    }

    public void Lock()
    {
      
        Vector3 launchVelocity = arrowLauncher != null ? arrowLauncher.GetLaunchDirection() : Vector3.zero;
        if (launchVelocity != Vector3.zero)
        {
            Vector3 flatDirection = new Vector3(launchVelocity.x, 0, launchVelocity.z);
            if (flatDirection.sqrMagnitude > 0.01f)
                _targetRotation = Quaternion.LookRotation(flatDirection);
        }
        else
        {
            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, Mathf.Infinity, ~_groundLayers) && _gamepad == null)
                _targetRotation = Quaternion.LookRotation(new Vector3(hit.point.x - transform.position.x, 0, hit.point.z - transform.position.z));
        }

        _animator.SetBool("Aiming", true);
        if (kControl != null) kControl.iKactive = true;
    }

    public void ResetAimingState()
    {
       
        if (kControl != null) kControl.iKactive = false;
        _animator.SetBool("Aiming", false);
    }
    
}