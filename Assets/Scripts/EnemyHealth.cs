using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    private EnemyBasic enemyBasic;
    private NavMeshAgent agent;
    private float enemyHealth = 100;
    private float currentHealth;
    private bool isDead = false;
    private Animator anim;
    private Rigidbody[] _ragdollRigidbodies;
    private EnemyState _currentState = EnemyState.Seeking;
    private enum EnemyState
    {
        Seeking,
        Ragdoll
    }
    private void Start()
    {
        enemyBasic = GetComponent<EnemyBasic>();
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        currentHealth = enemyHealth;
        _ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        DisableRagdoll();
    }

    public void TakeDamage(float damage, Vector3 force, Vector3 hitPoint)
    {
        currentHealth -= damage;
        enemyBasic.hitted = true;
        enemyBasic.Invoke("LateEndDizzy", 1);
        if (currentHealth <= 0 && !isDead)
        {
            Die(force, hitPoint);
            
        }
    }
    public void Die(Vector3 force, Vector3 hitPoint)
    {
        isDead = true;
        //enemyBasic.dead = isDead;
        enemyBasic.LateHideProjectileCurve();
        enemyBasic.StopAllCoroutines();
        enemyBasic.enabled = false;
        agent.enabled = false;
        
        TriggerRagdoll(force, hitPoint);
    }

    public void TriggerRagdoll(Vector3 force, Vector3 hitPoint)
    {
        EnableRagdoll();

        Rigidbody hitRigidbody = _ragdollRigidbodies.OrderBy(rigidbody => Vector3.Distance(rigidbody.position, hitPoint)).First();

        hitRigidbody.AddForceAtPosition(force, hitPoint, ForceMode.Impulse);

        //_currentState = ZombieState.Ragdoll;
    }

    private void DisableRagdoll()
    {
        foreach (var rigidbody in _ragdollRigidbodies)
        {
            rigidbody.isKinematic = true;
        }

        anim.enabled = true;
        enemyBasic.enabled = true;
    }

    private void EnableRagdoll()
    {
        foreach (var rigidbody in _ragdollRigidbodies)
        {
            rigidbody.isKinematic = false;
        }

        anim.enabled = false;
        enemyBasic.enabled = false;
    }

}
