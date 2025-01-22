using UnityEngine;

public class EnemyColliders : MonoBehaviour
{
    public EnemyHealth enemyHealth;
    public float damage = 10;
    public float force = 10;
  
    private void OnCollisionEnter(Collision collision)
    {

        if (collision.collider.CompareTag("Arrow"))
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                // Get the hit position
                Vector3 hitPosition = contact.point;
                enemyHealth.TakeDamage(damage,collision.collider.transform.forward * force, hitPosition);

            }
        }
    }
}
