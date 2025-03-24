using StarterAssets;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour, IHealable, IDamagable
{
    public float health = 100;
    public float currentHealth;
    private TopDownCharacter playerController;

    private NetworkVariable<HealthUpdate> healthVariable = new NetworkVariable<HealthUpdate>(new HealthUpdate { Health = 100, dead = false },
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private List<ISeekable> seekableList = new List<ISeekable>();
    ThirdPersonController thirdPersonController;
    public struct HealthUpdate : INetworkSerializable
    {

        public float Health;
        public bool dead;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Health); serializer.SerializeValue(ref dead);
        }
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        healthVariable.OnValueChanged += (oldValue, newValue) => OnValueChange();
    }

    void OnValueChange()
    {
        Debug.Log($"Health updated: {healthVariable.Value.Health}, Dead: {healthVariable.Value.dead}");

        currentHealth = healthVariable.Value.Health;
        if (healthVariable.Value.dead)
        {
            Death();
        }
    }


    private void Start()
    {
        currentHealth = health;
        playerController = GetComponent<TopDownCharacter>();
        thirdPersonController = GetComponent<ThirdPersonController>();
    }

   public void Damage(float damage)
{
    if (!IsServer) return; // Ensure only server modifies health

    Debug.Log($"PlayerHealth: Taking {damage} damage, current health: {currentHealth}");

    if (currentHealth > 0)
    {
        currentHealth -= damage;
        healthVariable.Value = new HealthUpdate { Health = currentHealth, dead = currentHealth <= 0 };

        if (currentHealth <= 0)
        {
            Debug.Log("Player is dead");
            Death();
        }
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
        thirdPersonController.enabled = false;
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
