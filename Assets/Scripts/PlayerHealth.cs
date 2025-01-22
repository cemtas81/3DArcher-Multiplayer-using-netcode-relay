using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IHealable, IDamagable
{
    public float health = 100;
    private float currentHealth;
    private TopDownCharacter playerController;
    private List<ISeekable> seekableList = new List<ISeekable>();

    private void Start()
    {
        currentHealth = health;
        playerController = GetComponent<TopDownCharacter>();

    }

    public void Damage(float damage)
    {
        if (currentHealth > 0)
        {
            currentHealth -= damage;
        }
        else
        {
            Death();
        }
    }

    public void Heal(float heal)
    {
        Debug.Log("Player heals: " + heal);
    }

    void Death()
    {
        Debug.LogWarning("Player is dead");
        playerController.enabled = false;
        UpdateSeekableList(); // Initialize the list
        foreach (var seekable in seekableList)
        {
            seekable.Seek(transform.position); // Notify each ISeekable
        }
    }

    public void UpdateSeekableList()
    {
        // Update the list by finding all objects of type ISeekable
        seekableList = FindObjectsOfType<MonoBehaviour>().OfType<ISeekable>().ToList();
    }
  
}
