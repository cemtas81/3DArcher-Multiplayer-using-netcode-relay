
using ProjectileCurveVisualizerSystem;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBasic : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator anim;
    public float radius;
    public bool canShoot = false, hitted = false, alerted;
    public float launchSpeed = 10.0f, dizzyDuration = 1;
    private Transform npcTransform, characterTransform;
    private Vector3 throwerVelocity, previousPosition;
    private readonly int _xTrigger = Animator.StringToHash("VelocityX");
    private readonly int _zTrigger = Animator.StringToHash("VelocityZ");
    private ProjectileCurveVisualizer projectileCurveVisualizer;
    public GameObject projectileGameObject;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        npcTransform = transform;
        characterTransform = FindFirstObjectByType<TopDownCharacter>().transform;
        previousPosition = npcTransform.position;
        StartCoroutine(StartWalking());
    }

    private IEnumerator StartWalking()
    {
        while (true)
        {
            SetRandomDestination();
            yield return new WaitUntil(() => !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance);
            canShoot = true;
            yield return new WaitForSeconds(3);
            canShoot = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!hitted && collision.collider.CompareTag("Arrow"))
        {
            hitted = true;
            Invoke(nameof(LateEndDizzy), dizzyDuration);
        }
    }

    private void LateEndDizzy() => hitted = false;

    private void Update()
    {
        throwerVelocity = (npcTransform.position - previousPosition) / Time.deltaTime;
        previousPosition = npcTransform.position;
        Animating(agent.velocity.x, agent.velocity.z);
        if (canShoot && alerted) Shoot();
    }

    private void Shoot()
    {
        if (hitted) return;

        if (projectileCurveVisualizer.VisualizeProjectileCurveWithTargetPosition(npcTransform.position, 1.5f, characterTransform.position, launchSpeed, throwerVelocity, Attributes.characterVelocity, 0.05f, 0.1f, false, out Vector3 updatedProjectileStartPosition, out Vector3 projectileLaunchVelocity, out Vector3 predictedTargetPosition, out RaycastHit hit))
        {
            if (throwerVelocity.magnitude > 0.1f) projectileCurveVisualizer.HideProjectileCurve();
            npcTransform.LookAt(updatedProjectileStartPosition);

            Projectile projectile = Instantiate(projectileGameObject).GetComponent<Projectile>();
            projectile.transform.position = updatedProjectileStartPosition;
            projectile.GetComponent<PaperBall>().enabled = true;
            projectile.Throw(projectileLaunchVelocity + throwerVelocity);
            Invoke(nameof(LateHideProjectileCurve), 4.0f);
        }
        else
        {
            projectileCurveVisualizer.HideProjectileCurve();
        }
    }

    private void LateHideProjectileCurve() => projectileCurveVisualizer.HideProjectileCurve();

    private void Animating(float h, float v)
    {
        bool walking = h != 0f || v != 0f;
        anim.SetBool("IsWalking", walking);
        Vector3 direction = new(h, 0, v);
        anim.SetFloat(_xTrigger, Vector3.Dot(direction.normalized, transform.right));
        anim.SetFloat(_zTrigger, Vector3.Dot(direction.normalized, transform.forward));
    }

    public void SetRandomDestination()
    {
        Vector3 randomPoint = GetRandomPoint(transform.position, radius);
        if (randomPoint != Vector3.zero) agent.SetDestination(randomPoint);
    }

    private Vector3 GetRandomPoint(Vector3 center, float radius)
    {
        Vector3 randomPos = center + Random.insideUnitSphere * radius;
        randomPos.y = center.y;

        if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return Vector3.zero;
    }

    public void Detected(Transform other) => characterTransform = other.transform;
}
