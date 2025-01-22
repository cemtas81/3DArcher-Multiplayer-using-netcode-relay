
using ProjectileCurveVisualizerSystem;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
public class EnemyBasic : MonoBehaviour,ISeekable
{
    private NavMeshAgent agent;
    private Animator anim;
    public float radius;
    private float range;
    private readonly int _xTrigger = Animator.StringToHash("VelocityX");
    private readonly int _zTrigger = Animator.StringToHash("VelocityZ");
    public bool canShoot = false, hitted = false;
    public float launchSpeed = 10.0f, dizzyDuration = 1;
    public bool alerted;
    bool canHitTarget = false;
    private Transform npcTransform;

    [Header("Shooting Settings")]
    public float shootCooldown = 2f;    // Time between shots
    private float nextShootTime = 0f;
    //private bool isAimed = false;
    private Transform characterTransform;
    private float shootTime;
    public float aimTime = 1.0f;    
    private Vector3 throwerVelocity;
    private Vector3 previousPosition;
    public float throwFrequency = 2.0f;
    // Output variables of method VisualizeProjectileCurveWithTargetPosition
    private Vector3 updatedProjectileStartPosition;
    private Vector3 projectileLaunchVelocity;
    private Vector3 predictedTargetPosition;
    private RaycastHit hit;
    public EnemyHealth enemyHealth;
    public ProjectileCurveVisualizer projectileCurveVisualizer;
    public bool dead=false;
    public GameObject projectileGameObject;
    private float backupTime;
    public Transform shootPos;
    private float shootDelay = 2.0f; // Delay between shots
    private float lastShootTime;
    private bool isShooting;
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        npcTransform = this.transform;
        characterTransform = FindFirstObjectByType<TopDownCharacter>().transform;
        previousPosition = npcTransform.position;
        enemyHealth = GetComponent<EnemyHealth>();
        //range = Random.Range(1, 3);
        StartCoroutine(StartWalking());
    }
    public void Seek(Vector3 target)
    {
        StopAllCoroutines();    
        agent.SetDestination(target);
        canHitTarget = false;
        canShoot = false;
    }
    IEnumerator StartWalking()
    {
        while (!dead)
        {
            SetRandomDestination();

            // Wait until the agent reaches the destination
            yield return new WaitUntil(() => !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance);
            canShoot = true;
            yield return new WaitForSeconds(3);
            canShoot = false;

        }
    }

    void LateEndDizzy()
    {
        hitted = false;

    }
  
    private void Update()
    {
       if (!dead)
        {
            // Calculate velocity for self
            throwerVelocity = (npcTransform.position - previousPosition) / Time.deltaTime;
            previousPosition = npcTransform.position;
            Animating(agent.velocity.x, agent.velocity.z);

            if (canShoot && alerted)
            {
                if (Time.time >= shootTime)   // Check if enough time has passed
                {
                    // Shoot at the player
                    Aim();

                    // Set the next time we can shoot
                    shootTime = Time.time + aimTime;
                }
            }
            else
            {
                projectileCurveVisualizer.HideProjectileCurve();
            }
        }
        
    }
    void Aim()
    {
        if (hitted) return;
        
        // Continuously aim and visualize trajectory - this should happen as long as canShoot is true
        canHitTarget = projectileCurveVisualizer.VisualizeProjectileCurveWithTargetPosition(
            shootPos.position,
            1f,
            characterTransform.position,
            launchSpeed,
            throwerVelocity,
            Attributes.characterVelocity,
            0.05f,
            0.1f,
            false,
            out updatedProjectileStartPosition,
            out projectileLaunchVelocity,
            out predictedTargetPosition,
            out hit);

        if (throwerVelocity.magnitude > 0.1f)
        {
            projectileCurveVisualizer.HideProjectileCurve();
            return;
        }

        npcTransform.LookAt(new Vector3(updatedProjectileStartPosition.x,npcTransform.position.y, updatedProjectileStartPosition.z));
        // Start shooting sequence if conditions are met
        if (canHitTarget && canShoot && !hitted && !isShooting)
        {
            StartCoroutine(ShootAfterAim());
        }
    }

    private IEnumerator ShootAfterAim()
    {
        
        if (Time.time < nextShootTime) yield break;  // Double check cooldown

        isShooting = true;  // Enter shooting state

        // Small delay to ensure proper aiming
        yield return new WaitForSeconds(1f);

        // Check again if conditions are still met
        if (canHitTarget && canShoot && !hitted&&!dead)
        {
            // Create and setup projectile
            Projectile projectile = Instantiate(projectileGameObject).GetComponent<Projectile>();
            projectile.transform.position = updatedProjectileStartPosition;
            projectile.Throw(projectileLaunchVelocity + throwerVelocity);

            // Set next shoot time
            nextShootTime = Time.time + shootCooldown;

            Invoke(nameof(LateHideProjectileCurve), 4.0f);
        }
        else
        {
            projectileCurveVisualizer.HideProjectileCurve();

        }
        // Add a small delay before allowing next shot
        yield return new WaitForSeconds(2f);
        isShooting = false;  // Exit shooting state
    }

    public void LateHideProjectileCurve()
    {
        projectileCurveVisualizer.HideProjectileCurve();
    }
    void Animating(float h, float v)
    {
        bool walking = h != 0f || v != 0f;
        anim.SetBool("IsWalking", walking);
        Vector3 direction = new(h, 0, v);
        float velocityZ = Vector3.Dot(direction.normalized, transform.forward);
        float velocityX = Vector3.Dot(direction.normalized, transform.right);
        anim.SetFloat(_xTrigger, velocityX);
        anim.SetFloat(_zTrigger, velocityZ);
    }
    public void SetRandomDestination()
    {
        Vector3 randomPoint = GetRandomPoint(transform.position, radius);
        if (randomPoint != Vector3.zero)
        {
            agent.SetDestination(randomPoint);
        }
    }

    private Vector3 GetRandomPoint(Vector3 center, float radius)
    {
        // Generate a random point within the radius
        Vector3 randomPos = center + Random.insideUnitSphere * radius;
        randomPos.y = center.y; // Adjust Y-axis for a flat plane (if necessary)

        // Check if the point is on the NavMesh
        if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            return hit.position; // Return the valid position on the NavMesh
        }

        return Vector3.zero; // Return zero if no valid position is found
    }
    public void Detected(Transform other)
    {

        characterTransform = other.transform;
    }
}
