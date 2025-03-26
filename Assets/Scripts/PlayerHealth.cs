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
            serializer.SerializeValue(ref Health);
            serializer.SerializeValue(ref dead);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        healthVariable.OnValueChanged += (oldValue, newValue) => OnValueChange(newValue);
    }

    void OnValueChange(HealthUpdate newValue)
    {
        Debug.Log($"Health updated: {newValue.Health}, Dead: {newValue.dead}");

        currentHealth = newValue.Health;
        if (newValue.dead)
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
        DamageServerRpc(damage);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DamageServerRpc(float damage)
    {
        // Apply damage to THIS player (already has their NetworkObject)
        ApplyDamageClientRpc(damage, OwnerClientId);
    }

    [ClientRpc]
    private void ApplyDamageClientRpc(float damage, ulong targetClientId)
    {
        if (IsOwner) // Only the owner modifies their health
        {
            var newHealth = healthVariable.Value.Health - damage;
            healthVariable.Value = new HealthUpdate
            {
                Health = Mathf.Max(0, newHealth),
                dead = newHealth <= 0
            };
        }
    }

    public void Heal(float heal)
    {
        Debug.Log("Player heals: " + heal);
    }

    void Death()
    {
        Debug.LogWarning("Player is dead");
        if (!IsOwner) return; // Only the owner disables their controls
        if (playerController != null) playerController.enabled = false;
        if (thirdPersonController != null) thirdPersonController.enabled = false;

        // UpdateSeekableList(); // Initialize the list
        // foreach (var seekable in seekableList)
        // {
        //     seekable.Seek(transform.position); // Notify each ISeekable
        // }
    }

    public void UpdateSeekableList()
    {
        // Update the list by finding all objects of type ISeekable
        seekableList = FindObjectsOfType<MonoBehaviour>().OfType<ISeekable>().ToList();
    }
}