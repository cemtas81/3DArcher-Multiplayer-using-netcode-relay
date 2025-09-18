using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ArrowFollowTrajectory : NetworkBehaviour
{
    [Header("Ayarlar")]
    [SerializeField] public float arrowSpeed = 20f, delay;
    [SerializeField] private bool autoRotate = true;
    [SerializeField] private float rotationSmoothing = 10f;

    [Header("Çarpýþma/Hasar/Saplanma")]
    [SerializeField] private float damage = 20f;
    [SerializeField] private float stickDuration = 3f;
    [SerializeField] private bool stickToHitTarget = true;
    [SerializeField] private float surfaceBackOffset = 0.02f;

    [Header("Çarpýþma Ayarlarý")]
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
    [SerializeField] private bool useSphereCast = false;
    [SerializeField] private float sphereRadius = 0.05f;

    [Header("Efekt")]
    [SerializeField] private GameObject effectPrefab;
    private NetworkObject currentEffect;
    private readonly NetworkVariable<ulong> effectObjectId = new NetworkVariable<ulong>();

    [Header("Görsel (Yerel)")]
    [SerializeField] private LineRenderer trajectoryLine; // Sadece yerel niþan göstergesi için

    [Header("Að Senkronu")]
    [SerializeField] private float interpolationTime = 0.1f;
    [SerializeField] private float netSyncRate = 30f; // saniyede kaç kez state yayalým
    private float netSyncTimer;

    // Að deðiþkenleri (sunucu yayar, istemciler okur)
    private readonly NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
    private readonly NetworkVariable<Vector3> networkVelocity = new NetworkVariable<Vector3>();
    private readonly NetworkVariable<int> networkPointIndex = new NetworkVariable<int>();

    // Deterministik yörünge verileri (parametreler)
    private Vector3 initStart;
    private Vector3 initVelocity;
    private int initPointCount;
    private float initTimeStep;
    private float initGravityMul = 1f;

    // Üretilmiþ yörünge ve hareket durumu
    private Vector3[] trajectoryPoints;      // Yörünge noktalarý
    private int currentPointIndex = 0;       // Sunucu/yerel hedef nokta indeksi
    private bool isMoving = false;

    // Ýstemci tarafý tahmin (prediction)
    private Vector3 clientPredictedPosition;
    private Vector3 clientPredictedVelocity;
    private int clientPredictedIndex = 0;

    private void Awake()
    {
        enabled = false; // sadece ateþ edildiðinde aktif
    }

    private void OnEnable()
    {
        if (IsClient)
        {
            networkPosition.OnValueChanged += OnPositionChanged;
            networkVelocity.OnValueChanged += OnVelocityChanged;
            networkPointIndex.OnValueChanged += OnPointIndexChanged;
        }
        effectObjectId.OnValueChanged += OnEffectObjectIdChanged;
        TryAssignEffect();
    }

    private void OnDisable()
    {
        if (IsClient)
        {
            networkPosition.OnValueChanged -= OnPositionChanged;
            networkVelocity.OnValueChanged -= OnVelocityChanged;
            networkPointIndex.OnValueChanged -= OnPointIndexChanged;
        }
        effectObjectId.OnValueChanged -= OnEffectObjectIdChanged;

        if (IsServer && currentEffect != null)
        {
            currentEffect.Despawn();
            currentEffect = null;
        }
    }

    // Mevcut API (sunucuda kullanýlmalý)
    public void StartFollowingTrajectory(LineRenderer lineRenderer)
    {
        if (!IsServer)
        {
            Debug.LogWarning("StartFollowingTrajectory(LineRenderer) sadece sunucuda kullanýlmalýdýr. Parametreli baþlatmayý kullanýn.");
            return;
        }

        if (lineRenderer == null || lineRenderer.positionCount < 2)
        {
            Debug.LogWarning("Geçerli bir yörünge çizgisi bulunamadý!");
            return;
        }

        trajectoryPoints = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(trajectoryPoints);

        // Varsayýlan parametreleri tahmin et
        initStart = trajectoryPoints[0];
        Vector3 firstDir = (trajectoryPoints[1] - trajectoryPoints[0]).normalized;
        initVelocity = firstDir * Mathf.Max(arrowSpeed, 0.01f);
        initPointCount = trajectoryPoints.Length;
        initTimeStep = 0.0333f;
        initGravityMul = 1f;

        ServerBeginMovementAndBroadcast();
    }

    // Tercih edilen baþlatma (sunucu tarafýnda deterministik parametrelerle)
    public void StartFollowingTrajectoryParams(Vector3 start, Vector3 launchVelocity, int points, float timeStep, float gravityMul)
    {
        if (!IsServer)
        {
            Debug.LogWarning("StartFollowingTrajectoryParams sadece sunucuda çaðrýlmalýdýr.");
            return;
        }

        initStart = start;
        initVelocity = launchVelocity;
        initPointCount = Mathf.Max(points, 2);
        initTimeStep = Mathf.Max(timeStep, 0.0001f);
        initGravityMul = Mathf.Max(gravityMul, 0f);

        trajectoryPoints = GenerateTrajectoryPoints(initStart, initVelocity, initPointCount, initTimeStep, initGravityMul);
        ServerBeginMovementAndBroadcast();
    }

    private void ServerBeginMovementAndBroadcast()
    {
        currentPointIndex = 1;
        transform.position = trajectoryPoints[0];
        isMoving = true;
        enabled = true;

        // Ýlk network state
        networkPosition.Value = transform.position;
        networkVelocity.Value = (trajectoryPoints[1] - trajectoryPoints[0]).normalized * arrowSpeed;
        networkPointIndex.Value = currentPointIndex;

        // Efekt: sunucuda spawn + id yayýnla
        if (effectPrefab != null)
        {
            currentEffect = NetworkProjectilePool.Singleton.GetNetworkObject(effectPrefab, transform.position, transform.rotation);
            currentEffect.Spawn(true);
            effectObjectId.Value = currentEffect.NetworkObjectId;
        }

        // Ýstemcilere parametre yayýný
        InitClientsClientRpc(initStart, initVelocity, initPointCount, initTimeStep, initGravityMul, arrowSpeed, autoRotate, rotationSmoothing);
    }

    [ClientRpc]
    private void InitClientsClientRpc(
        Vector3 start,
        Vector3 launchVelocity,
        int points,
        float timeStep,
        float gravityMul,
        float speed,
        bool autoRot,
        float rotSmooth)
    {
        initStart = start;
        initVelocity = launchVelocity;
        initPointCount = Mathf.Max(points, 2);
        initTimeStep = Mathf.Max(timeStep, 0.0001f);
        initGravityMul = Mathf.Max(gravityMul, 0f);

        arrowSpeed = speed;
        autoRotate = autoRot;
        rotationSmoothing = rotSmooth;

        trajectoryPoints = GenerateTrajectoryPoints(initStart, initVelocity, initPointCount, initTimeStep, initGravityMul);

        clientPredictedIndex = 1;
        clientPredictedPosition = trajectoryPoints[0];
        clientPredictedVelocity = (trajectoryPoints[1] - trajectoryPoints[0]).normalized * arrowSpeed;

        transform.position = clientPredictedPosition;
        if (autoRotate) LookAtDirection(clientPredictedVelocity);

        isMoving = true;
        enabled = true;

        // Efekt id atanmýþsa baðlamayý dene
        TryAssignEffect();
    }

    private void FixedUpdate()
    {
        if (!isMoving || trajectoryPoints == null || trajectoryPoints.Length < 2)
            return;

        if (IsServer)
        {
            ServerMoveAlongTrajectory();
        }
        else if (IsClient)
        {
            PredictMovement();
            InterpolateToServerState();
        }

        // Efekt takip (her iki tarafta da)
        if (currentEffect != null)
            currentEffect.transform.SetPositionAndRotation(transform.position, transform.rotation);
    }

    // Sunucu otoritesi: yörüngede sabit hýzla ilerlet + çarpýþma/hasar/saplanma
    private void ServerMoveAlongTrajectory()
    {
        float remaining = arrowSpeed * Time.fixedDeltaTime;

        while (remaining > 0f && currentPointIndex < trajectoryPoints.Length)
        {
            Vector3 target = trajectoryPoints[currentPointIndex];
            Vector3 toTarget = target - transform.position;
            float dist = toTarget.magnitude;

            if (dist < 1e-5f)
            {
                currentPointIndex++;
                continue;
            }

            float step = Mathf.Min(remaining, dist);
            Vector3 dir = toTarget / dist;

            // Çarpýþma kontrolü (adým kadar ray/sphere cast)
            if (CastForHit(transform.position, dir, step, out RaycastHit hit))
            {
                // Ýsabet konumu ve yön
                Vector3 hitPos = hit.point - dir * surfaceBackOffset;
                transform.position = hitPos;
                if (autoRotate) LookAtDirection(dir * arrowSpeed);

                // Að durumunu kesin konuma it (anýnda)
                networkPosition.Value = transform.position;
                networkVelocity.Value = Vector3.zero;
                networkPointIndex.Value = currentPointIndex;

                // Hasar uygula (IDamagable varsa)
                if (hit.collider != null && hit.collider.TryGetComponent<IDamagable>(out var damagable))
                {
                    damagable.Damage(damage, hit.point);
                    if (hit.collider.TryGetComponent<NetworkObject>(out var targetNet))
                    {
                        NotifyDamageClientRpc(targetNet.NetworkObjectId, damage);
                    }
                }

                // Saplanma: hedefe yapýþtýr (opsiyonel)
                ulong parentId = 0;
                bool hasParent = false;
                if (stickToHitTarget && hit.collider != null && hit.collider.TryGetComponent<NetworkObject>(out var parentNet))
                {
                    transform.SetParent(parentNet.transform, true);
                    hasParent = true;
                    parentId = parentNet.NetworkObjectId;
                }

                // Efekti oka baðla ki saplanýnca birlikte kalsýn
                if (currentEffect != null)
                    currentEffect.transform.SetParent(transform, true);

                // Ýstemcilere "stuck" durumu yayýnla
                StuckClientRpc(transform.position, transform.forward, hasParent, parentId);

                // Hareketi durdur
                isMoving = false;

                // Bir süre bekleyip yok et
                StartCoroutine(DestroyAfterDelay(stickDuration));
                return;
            }

            // Hareket
            transform.position += dir * step;
            remaining -= step;

            // Görsel yön
            if (autoRotate) LookAtDirection(dir * arrowSpeed);

            // Að güncellemesini throttle et
            netSyncTimer += step; // kat edilen mesafe ile orantýlý; dilersen Time.fixedDeltaTime kullan
            if (netSyncTimer >= (arrowSpeed / Mathf.Max(arrowSpeed, 0.0001f)) * (1f / netSyncRate))
            {
                networkPosition.Value = transform.position;
                networkVelocity.Value = dir * arrowSpeed;
                networkPointIndex.Value = currentPointIndex;
                netSyncTimer = 0f;
            }

            if (step >= dist - 1e-6f)
                currentPointIndex++;
        }

        if (currentPointIndex >= trajectoryPoints.Length)
        {
            OnTrajectoryCompleted();
        }
    }

    private bool CastForHit(Vector3 origin, Vector3 dir, float distance, out RaycastHit hit)
    {
        if (useSphereCast)
            return Physics.SphereCast(origin, sphereRadius, dir, out hit, distance, hitMask, triggerInteraction);
        else
            return Physics.Raycast(origin, dir, out hit, distance, hitMask, triggerInteraction);
    }

    [ClientRpc]
    private void StuckClientRpc(Vector3 pos, Vector3 forward, bool hasParent, ulong parentId)
    {
        // Ýstemciler: sunucu konum/rotasyonuna atla ve tahmini durdur
        transform.SetPositionAndRotation(pos, Quaternion.LookRotation(forward, Vector3.up));
        isMoving = false;

        // Hedefe parentla (varsa)
        if (hasParent && parentId != 0 && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(parentId, out var parentObj))
        {
            transform.SetParent(parentObj.transform, true);
        }

        // Efekti oka baðla ki sabit kalsýn
        if (currentEffect != null)
            currentEffect.transform.SetParent(transform, true);
    }

    private void OnEffectObjectIdChanged(ulong oldId, ulong newId)
    {
        TryAssignEffect();
    }

    private void TryAssignEffect()
    {
        if (IsClient && effectObjectId.Value != 0 && currentEffect == null)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(effectObjectId.Value, out NetworkObject netObj))
            {
                currentEffect = netObj;
                // Ýlk atamada poz/rot eþitle
                currentEffect.transform.SetPositionAndRotation(transform.position, transform.rotation);
            }
        }
    }

    [ClientRpc]
    private void NotifyDamageClientRpc(ulong targetId, float dmg)
    {
        Debug.Log($"Damage applied to {targetId}: {dmg}");
    }

    // Ýstemci tarafý tahmin
    private void PredictMovement()
    {
        if (clientPredictedIndex < 1) clientPredictedIndex = 1;

        float remaining = arrowSpeed * Time.fixedDeltaTime;

        while (remaining > 0f && clientPredictedIndex < trajectoryPoints.Length)
        {
            Vector3 target = trajectoryPoints[clientPredictedIndex];
            Vector3 toTarget = target - clientPredictedPosition;
            float dist = toTarget.magnitude;

            if (dist < 1e-5f)
            {
                clientPredictedIndex++;
                continue;
            }

            float step = Mathf.Min(remaining, dist);
            Vector3 dir = toTarget / dist;

            clientPredictedPosition += dir * step;
            clientPredictedVelocity = dir * arrowSpeed;
            remaining -= step;

            if (step >= dist - 1e-6f)
                clientPredictedIndex++;
        }

        transform.position = clientPredictedPosition;
        if (autoRotate) LookAtDirection(clientPredictedVelocity);

        // Sunucu indeksi bizden öndeyse atlama yap
        if (networkPointIndex.Value > clientPredictedIndex)
            clientPredictedIndex = networkPointIndex.Value;
    }

    // Sunucu durumuna doðru yumuþak düzeltme
    private void InterpolateToServerState()
    {
        transform.position = Vector3.Lerp(transform.position, networkPosition.Value, interpolationTime);
        clientPredictedVelocity = Vector3.Lerp(clientPredictedVelocity, networkVelocity.Value, interpolationTime);
    }

    private void OnPositionChanged(Vector3 oldPosition, Vector3 newPosition) { }
    private void OnVelocityChanged(Vector3 oldVelocity, Vector3 newVelocity) { }
    private void OnPointIndexChanged(int oldIndex, int newIndex)
    {
        if (newIndex > clientPredictedIndex) clientPredictedIndex = newIndex;
    }

    private void LookAtDirection(Vector3 velocityOrDir)
    {
        Vector3 dir = velocityOrDir;
        if (dir.sqrMagnitude < 0.0001f) return;

        dir.Normalize();
        Quaternion targetRotation = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothing);
    }

    private void OnTrajectoryCompleted()
    {
        isMoving = false;
        enabled = false;
        StartCoroutine(DestroyAfterDelay(delay));
    }

    public bool IsMoving() => isMoving;

    public void StopMoving()
    {
        isMoving = false;
        enabled = false;
        StopAllCoroutines();
        StartCoroutine(DestroyAfterDelay(delay));
    }

    private IEnumerator DestroyAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        if (IsServer)
        {
            if (currentEffect != null)
            {
                currentEffect.Despawn();
                currentEffect = null;
            }
            var netObj = GetComponent<NetworkObject>();
            netObj.Despawn();
        }
    }

    // Deterministik yörünge üretimi
    private Vector3[] GenerateTrajectoryPoints(Vector3 start, Vector3 velocity, int points, float timeStep, float gravityMul)
    {
        int count = Mathf.Max(points, 2);
        var result = new Vector3[count];
        Vector3 g = Physics.gravity * gravityMul;

        for (int i = 0; i < count; i++)
        {
            float t = i * timeStep;
            result[i] = start + velocity * t + 0.5f * g * (t * t);
        }
        return result;
    }
}