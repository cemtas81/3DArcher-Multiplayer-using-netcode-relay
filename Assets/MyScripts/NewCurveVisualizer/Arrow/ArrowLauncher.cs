using Unity.Netcode;
using UnityEngine;

public class ArrowLauncher : NetworkBehaviour
{
    [Header("Arrow Settings")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform arrowSpawnPoint;
    [SerializeField] private float maxPower = 25f;
    [SerializeField] private float powerMultiplier = 2f;
    [SerializeField] private float straightArrowSpeedMultiplier = 2f; // Düz atýþta hýz çarpaný

    [Header("Visual")]
    [SerializeField] private LineRenderer trajectoryLine;
    [SerializeField] private int trajectoryPoints = 50;
    [SerializeField] private float trajectoryTimeStep = 0.1f;

    [Header("Crosshair Settings")]
    [SerializeField] private GameObject crosshairPrefab;
    [SerializeField] private float crosshairSize = 0.5f;
    [SerializeField] private LayerMask layerMask = ~0; // Default to everything
    private GameObject crosshairInstance;
    [SerializeField] private Transform aim;

    [Header("Isometric Settings")]
    [SerializeField] private bool useIsometricMode = true;

    [Header("Trajectory Curvature")]
    [SerializeField, Range(0.1f, 5f)] private float arcHeightMultiplier = 1f;
    [SerializeField, Range(0.1f, 3f)] private float gravityMultiplier = 1f;

    [Header("Flatten Settings")]
    [SerializeField] private float flattenStartDragDistance = 100f;
    [SerializeField] private float flattenMaxDragDistance = 300f;
    [SerializeField] private float minGravityMultiplier = 0.01f;
    [SerializeField] private float completelyFlatThreshold = 0.95f;

    [Header("Input Thresholds")]
    [SerializeField] private float minDragDistance = 12f; // Atýþ baþlamasý için minimum sürükleme (px)

    // Input / durum
    private Vector3 startMousePosition;
    public bool isAiming;
    private bool dragPassedThreshold;

    [SerializeField] private Camera mainCamera; // inspector'dan atanabilir
    [SerializeField] private TrajectoryPredictor trajectoryPredictor; // inspector'dan atanabilir

    // Hesaplanan ara deðerler (cache)
    private bool shotStateValid;
    private Vector3 lastLaunchVelocity;
    private Vector3 lastAdjustedVelocity;
    private float lastFlattenFactor;
    private float lastEffectiveGravity;
    private bool lastIsStraightShot;
    

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (trajectoryPredictor == null) trajectoryPredictor = FindFirstObjectByType<TrajectoryPredictor>();

        if (trajectoryLine == null) SetupTrajectoryLine();
        if (arrowSpawnPoint == null) arrowSpawnPoint = transform;
        CreateCrosshair();
    }

    private void SetupTrajectoryLine()
    {
        GameObject lineObj = new GameObject("ArrowTrajectoryLine");
        lineObj.transform.SetParent(transform);
        trajectoryLine = lineObj.AddComponent<LineRenderer>();

        trajectoryLine.material = new Material(Shader.Find("Sprites/Default"));
        trajectoryLine.startColor = Color.yellow;
        trajectoryLine.endColor = new Color(1f, 0.5f, 0f, 0.5f);
        trajectoryLine.startWidth = 0.05f;
        trajectoryLine.endWidth = 0.02f;
        trajectoryLine.positionCount = trajectoryPoints;
        trajectoryLine.enabled = false;
        trajectoryLine.useWorldSpace = true;
    }

    private void CreateCrosshair()
    {
        if (crosshairPrefab != null)
        {
            crosshairInstance = Instantiate(crosshairPrefab, transform);
        }
        else
        {
            crosshairInstance = new GameObject("Crosshair");
            crosshairInstance.transform.SetParent(transform);
            var sr = crosshairInstance.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("UI/Crosshair");
        }

        crosshairInstance.transform.localScale = Vector3.one * crosshairSize;
        crosshairInstance.SetActive(false);
    }

    private void Update()
    {
        if (!IsOwner) return;
        HandleInput();
        if (isAiming)
        {
            UpdateAiming();
        }
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartAiming();
        }
        else if (Input.GetMouseButtonUp(0) && isAiming)
        {
            FireArrow();
        }
    }

    private void StartAiming()
    {
        startMousePosition = Input.mousePosition;
        isAiming = true;
        shotStateValid = false;
        dragPassedThreshold = false; // Eþik henüz aþýlmadý
    }

    private void UpdateAiming()
    {
        // Eþik aþýlmadýysa kontrol et
        if (!dragPassedThreshold)
        {
            if (CurrentDragDistance() < minDragDistance)
                return; // Henüz hedefleme baþlamasýn

            dragPassedThreshold = true;
            if (trajectoryLine != null) trajectoryLine.enabled = true;
            if (crosshairInstance != null) crosshairInstance.SetActive(true);
        }

        ComputeShotState();
        PredictTrajectoryIfAvailable();
        UpdateCrosshairPosition();
    }

    private void FireArrow()
    {
        // Minimum sürükleme saðlanmadýysa atýþý iptal et
        if (!dragPassedThreshold)
        {
            CleanupAfterShot();
            return;
        }
        if (!shotStateValid)
            ComputeShotState(); // Çok hýzlý týklayýp býraktýysa emin ol

        // Geçersiz/çok küçük hýzlar için koruma
        if (lastAdjustedVelocity.sqrMagnitude < 1e-4f)
        {
            CleanupAfterShot();
            return;
        }

        PredictTrajectoryIfAvailable(); // Son bir kez (görsel tutarlýlýk)

        if (arrowPrefab == null) // Emniyet
        {
            CleanupAfterShot();
            return;
        }

        // Gerekli verileri sunucuya gönder
        Vector3 spawnRot = lastAdjustedVelocity.normalized;
        Vector3 spawnPos = arrowSpawnPoint.position;

        // Ýstemcide hesaplanan deterministik parametreleri sunucuya gönder
        SpawnArrowServerRpc(
            spawnPos,
            spawnRot,
            lastAdjustedVelocity,            // initialVelocity
            trajectoryPoints,                // pointsCount
            trajectoryTimeStep,              // timeStep
            lastEffectiveGravity,            // gravityMul
            lastIsStraightShot               // düz atýþ mý
        );

        // Client tarafýndaki niþan/çizgi temizlikleri burada yapýlmalý
        CleanupAfterShot();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnArrowServerRpc(
        Vector3 pos,
        Vector3 rot,
        Vector3 initialVelocity,
        int pointsCount,
        float timeStep,
        float gravityMul,
        bool isStraightShot)
    {
        // Get a pooled arrow instance from the pool
        NetworkObject arrowNetworkObject =
            NetworkProjectilePool.Singleton.GetNetworkObject(arrowPrefab, pos, Quaternion.LookRotation(rot));

        // Spawn the pooled arrow on the network
        arrowNetworkObject.Spawn(true);
        ArrowFollowTrajectory arrowController =
           arrowNetworkObject.GetComponent<ArrowFollowTrajectory>();

        if (isStraightShot)
            arrowController.arrowSpeed *= straightArrowSpeedMultiplier;

        // Deterministik parametrelerle sunucuda baþlat; istemcilere ClientRpc ile yayýnlanýr
        arrowController.StartFollowingTrajectoryParams(
            pos,
            initialVelocity,
            pointsCount,
            timeStep,
            gravityMul
        );
    }

    private void CleanupAfterShot()
    {
        isAiming = false;
        shotStateValid = false;
        dragPassedThreshold = false;
        if (trajectoryLine != null) trajectoryLine.enabled = false;
        if (crosshairInstance != null) crosshairInstance.SetActive(false);
    }

    // --- Hesaplama Akýþý ---
    private void ComputeShotState()
    {
        lastLaunchVelocity = CalculateLaunchVelocity();
        lastFlattenFactor = CalculateFlattenFactor();

        lastEffectiveGravity = Mathf.Lerp(gravityMultiplier, minGravityMultiplier, lastFlattenFactor);
        lastAdjustedVelocity = AdjustVelocityForFlattening(lastLaunchVelocity, lastFlattenFactor);

        lastIsStraightShot = false;
        if (lastFlattenFactor > completelyFlatThreshold)
        {
            float t = (lastFlattenFactor - completelyFlatThreshold) / (1f - completelyFlatThreshold);
            float speedMultiplier = Mathf.Lerp(1f, straightArrowSpeedMultiplier, t);
            lastAdjustedVelocity *= speedMultiplier;
            lastIsStraightShot = true;
        }

        shotStateValid = true;
    }

    private void PredictTrajectoryIfAvailable()
    {
        if (trajectoryPredictor == null || trajectoryLine == null) return;
        trajectoryPredictor.PredictTrajectory(
            arrowSpawnPoint.position,
            lastAdjustedVelocity,
            trajectoryLine,
            trajectoryPoints,
            trajectoryTimeStep,
            lastEffectiveGravity
        );
    }

    // --- Yardýmcý Metotlar ---
    private Vector3 CalculateLaunchVelocity()
    {
        Vector3 currentMouse = Input.mousePosition;
        Vector3 mouseDelta = startMousePosition - currentMouse;

        if (useIsometricMode)
            return CalculateIsometricLaunchVelocity(mouseDelta);

        // Isometric kapalý: Kamera derinliðine göre basit projeksiyon
        float depth = mainCamera.WorldToScreenPoint(arrowSpawnPoint.position).z;
        Vector3 startWorld = mainCamera.ScreenToWorldPoint(new Vector3(startMousePosition.x, startMousePosition.y, depth));
        Vector3 currentWorld = mainCamera.ScreenToWorldPoint(new Vector3(currentMouse.x, currentMouse.y, depth));
        Vector3 dragWorld = startWorld - currentWorld;

        Vector3 launchVelocity = dragWorld * powerMultiplier;
        return Vector3.ClampMagnitude(launchVelocity, maxPower);
    }

    private Vector3 CalculateIsometricLaunchVelocity(Vector3 mouseDelta)
    {
        Vector3 fwd = mainCamera.transform.forward; // Y bileþeni sýfýrlanarak yataya projekte edilir
        Vector3 right = mainCamera.transform.right;

        fwd.y = 0f;
        right.y = 0f;
        fwd.Normalize();
        right.Normalize();

        Vector3 horizontal = (mouseDelta.x * right + mouseDelta.y * fwd) * 0.01f;

        float dragMag = mouseDelta.magnitude;
        float vertical = Mathf.Clamp(dragMag * 0.01f * arcHeightMultiplier, 0f, maxPower * 0.5f);

        Vector3 v = horizontal * powerMultiplier;
        v.y = vertical * powerMultiplier;

        return Vector3.ClampMagnitude(v, maxPower);
    }

    private Vector3 AdjustVelocityForFlattening(Vector3 originalVelocity, float flattenFactor)
    {
        if (flattenFactor <= 0f) return originalVelocity;

        Vector3 flat = new Vector3(originalVelocity.x, 0f, originalVelocity.z);
        float speed = originalVelocity.magnitude;
        if (flat.sqrMagnitude > 1e-6f)
            flat = flat.normalized * speed;

        return Vector3.Lerp(originalVelocity, flat, flattenFactor);
    }

    private float CalculateFlattenFactor()
    {
        float dragDistance = (startMousePosition - Input.mousePosition).magnitude;
        if (dragDistance < flattenStartDragDistance) return 0f;
        if (dragDistance >= flattenMaxDragDistance) return 1f;
        float t = (dragDistance - flattenStartDragDistance) / (flattenMaxDragDistance - flattenStartDragDistance);
        return t * t * (3f - 2f * t); // smoothstep
    }

    private float CurrentDragDistance() => (startMousePosition - Input.mousePosition).magnitude;

    private void UpdateCrosshairPosition()
    {
        if (crosshairInstance == null || trajectoryLine == null || trajectoryLine.positionCount <= 0) return;

        Vector3 landing = trajectoryLine.GetPosition(trajectoryLine.positionCount - 1);
        Vector3 surfaceNormal = Vector3.up;

        if (trajectoryLine.positionCount > 1)
        {
            Vector3 prev = trajectoryLine.GetPosition(trajectoryLine.positionCount - 2);
            Vector3 dir = landing - prev;
            float dist = dir.magnitude;

            if (dist > 0.01f)
            {
                dir /= dist;
                if (Physics.Raycast(prev, dir, out RaycastHit hit, dist + 0.1f,layerMask))
                {
                    surfaceNormal = hit.normal;
                    landing = hit.point;
                }
            }
        }

        crosshairInstance.transform.SetPositionAndRotation(landing + surfaceNormal * 0.01f, Quaternion.FromToRotation(Vector3.forward, surfaceNormal));
        Vector3 peak=GetTrajectoryPeakPoint();
        aim.position=peak;
    }

    public Vector3 GetLaunchDirection()
    {
        // Aiming sýrasýnda güncel fýrlatma yönü; yoksa Vector3.zero döner
        return lastLaunchVelocity;
    }
    // LineRenderer'a eriþim saðlayan metod
    public LineRenderer GetTrajectoryLine()
    {
        return trajectoryLine;
    }

    // Trajectory'nin en yüksek (pik) noktasýný hesaplayan metod
    public Vector3 GetTrajectoryPeakPoint()
    {
        if (trajectoryLine == null || trajectoryLine.positionCount <= 0)
            return Vector3.zero;

        Vector3 highestPoint = trajectoryLine.GetPosition(0);
        float maxHeight = highestPoint.y;

        // Tüm noktalarý kontrol ederek en yüksek y deðerine sahip olaný bul
        for (int i = 1; i < trajectoryLine.positionCount; i++)
        {
            Vector3 point = trajectoryLine.GetPosition(i);
            if (point.y > maxHeight)
            {
                maxHeight = point.y;
                highestPoint = point;
            }
        }

        return highestPoint;
    }
}