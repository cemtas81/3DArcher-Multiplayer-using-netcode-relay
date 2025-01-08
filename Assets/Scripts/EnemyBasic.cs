
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
public class EnemyBasic : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator anim;
    public float radius;
    private float range;
    private readonly int _xTrigger = Animator.StringToHash("VelocityX");
    private readonly int _zTrigger = Animator.StringToHash("VelocityZ");
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
       
        //range = Random.Range(1, 3);
        StartCoroutine(StartWalking());
    }
    IEnumerator StartWalking()
    {
        while (true)
        {
            SetRandomDestination();

            // Wait until the agent reaches the destination
            yield return new WaitUntil(() => !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance);

            yield return new WaitForSeconds(3);
            
        }
    }
    private void Update()
    {
       
        Animating(agent.velocity.x, agent.velocity.z);
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
}
