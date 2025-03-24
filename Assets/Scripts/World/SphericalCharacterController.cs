using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SphericalCharacterController : MonoBehaviour
{
    public Transform planet; // Reference to the planet's transform
    public float gravityForce = 9.81f;
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public float rotationSpeed = 10f;

    private Rigidbody rb;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // Disable default Unity gravity
    }

    void FixedUpdate()
    {
        ApplyGravity();
        MovePlayer();
        AlignWithSurface();
    }

    void Update()
    {
        HandleJump();
    }

    void ApplyGravity()
    {
        Vector3 gravityDirection = (transform.position - planet.position).normalized;
        rb.AddForce(-gravityDirection * gravityForce, ForceMode.Acceleration);
    }

    void MovePlayer()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 surfaceNormal = (transform.position - planet.position).normalized;
        Vector3 forward = Vector3.Cross(transform.right, surfaceNormal).normalized;
        Vector3 right = Vector3.Cross(surfaceNormal, forward).normalized;

        Vector3 movement = (forward * moveVertical + right * moveHorizontal).normalized;
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    void AlignWithSurface()
    {
        Vector3 gravityDirection = (transform.position - planet.position).normalized;
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, gravityDirection) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    void HandleJump()
    {
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("World"))
        {
            isGrounded = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("World"))
        {
            isGrounded = false;
        }
    }
}