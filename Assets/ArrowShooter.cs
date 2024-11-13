using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ArrowShooter : MonoBehaviour
{
    public Transform arrowPrefab; // Prefab of the arrow to shoot
    public Transform shootPoint; // Point from where the arrow is shot
    public float maxLaunchForce = 30f; // Maximum launch force
    public float minLaunchForce = 5f;  // Minimum launch force
    public float trajectoryTime = 2f; // Duration for trajectory visualization

    private LineRenderer lineRenderer;
    private Vector3 initialMousePosition;
    private bool isAiming;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Update()
    {
        Aim();
        if (Input.GetMouseButtonDown(0))
        {
            // Record the initial mouse position when aiming starts
            initialMousePosition = Input.mousePosition;
            isAiming = true;
        }

        if (Input.GetMouseButton(0) && isAiming)
        {
            Aim();  // Continue aiming while holding the mouse button
        }

        if (Input.GetMouseButtonUp(0))
        {
            Shoot();  // Shoot the arrow when mouse button is released
            isAiming = false; // Reset aiming state
        }
    }

    void Aim()
    {
        // Calculate the mouse movement in the Y direction
        float mouseDeltaY = (Input.mousePosition.y - initialMousePosition.y) / Screen.height; // Normalize to screen height
        float launchForce = Mathf.Lerp(minLaunchForce, maxLaunchForce, Mathf.Clamp01(mouseDeltaY)); // Scale the force based on Y movement

        // Calculate the direction to aim
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Camera.main.WorldToScreenPoint(shootPoint.position).z;
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);

        Vector3 aimDirection = (worldPosition - shootPoint.position).normalized;

        // Draw the trajectory curve with the current launch force
        DrawTrajectory(aimDirection, launchForce);
    }

    void Shoot()
    {
        // Calculate the launch force based on mouse Y movement
        float mouseDeltaY = (Input.mousePosition.y - initialMousePosition.y) / Screen.height;
        float launchForce = Mathf.Lerp(minLaunchForce, maxLaunchForce, Mathf.Clamp01(mouseDeltaY));

        // Get the aim direction
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Camera.main.WorldToScreenPoint(shootPoint.position).z;
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);

        Vector3 aimDirection = (worldPosition - shootPoint.position).normalized;

        // Instantiate and launch the arrow
        Transform arrow = Instantiate(arrowPrefab, shootPoint.position, Quaternion.identity);
        Rigidbody arrowRb = arrow.GetComponent<Rigidbody>();
        arrowRb.linearVelocity = aimDirection * launchForce;
    }

    void DrawTrajectory(Vector3 direction, float launchForce)
    {
        int points = 20; // Number of points in the line renderer
        lineRenderer.positionCount = points;

        Vector3 startPosition = shootPoint.position;
        Vector3 startVelocity = direction * launchForce;

        for (int i = 0; i < points; i++)
        {
            float time = i / (float)points * trajectoryTime;
            Vector3 point = startPosition + startVelocity * time + 0.5f * Physics.gravity * time * time;
            lineRenderer.SetPosition(i, point);
        }
    }
}


