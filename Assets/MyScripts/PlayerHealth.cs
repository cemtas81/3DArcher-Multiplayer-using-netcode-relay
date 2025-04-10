
using StarterAssets;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour, IHealable, IDamagable
{
    public float health = 100;
    public float currentHealth;
    private TopDownCharacter playerController;
    private Animator anim;
    private NetworkVariable<HealthUpdate> healthVariable = new NetworkVariable<HealthUpdate>(new HealthUpdate { Health = 100, dead = false },
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private Rigidbody rb;
    private List<ISeekable> seekableList = new List<ISeekable>();
    ThirdPersonController thirdPersonController;
    private Rigidbody[] _ragdollRigidbodies;
    private ClientNetworkTransform networkTransform;
    Vector3 hitDir;


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
        currentHealth = health;

    }
    private void Start()
    {
        playerController = GetComponent<TopDownCharacter>();
        thirdPersonController = GetComponent<ThirdPersonController>();
        _ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        networkTransform = GetComponent<ClientNetworkTransform>();
        DisableRagdoll();
    }
    void OnValueChange(HealthUpdate newValue)
    {
        Debug.Log($"[{NetworkManager.Singleton.LocalClientId}] Health updated: {newValue.Health}, Dead: {newValue.dead}");

        currentHealth = newValue.Health;
        if (newValue.dead)
        {
            Death(hitDir);
        }
    }

    private void DisableRagdoll()
    {
        rb.isKinematic = false; // Set the main Rigidbody to non-kinematic
        foreach (var rigidbody in _ragdollRigidbodies)
        {
            rigidbody.isKinematic = true;
        }

        anim.enabled = true;
        networkTransform.enabled = true; // Enable the NetworkTransform component
        if (playerController != null) playerController.enabled = true;
        if (thirdPersonController != null) thirdPersonController.enabled = true;
    }
    private void EnableRagdoll(Vector3 forceDirection)
    {
     
        foreach (var rigidbody in _ragdollRigidbodies)
        {
            rigidbody.isKinematic = false;
        }
        networkTransform.enabled = false;
    }
    public void Damage(float damage, Vector3 hitPoint)
    {
        DamageServerRpc(damage, hitPoint);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DamageServerRpc(float damage, Vector3 hit)
    {
        float newHealth = healthVariable.Value.Health - damage;
        healthVariable.Value = new HealthUpdate
        {
            Health = Mathf.Max(0, newHealth),
            dead = newHealth <= 0
        };

        if (newHealth <= 0)
        {
            hitDir = hit;
            Death(hitDir);
        }

        // Ensure clients update their UI/logic
        ApplyDamageClientRpc(newHealth, healthVariable.Value.dead, hit);
    }

    [ClientRpc]
    private void ApplyDamageClientRpc(float newHealth, bool isDead, Vector3 hit)
    {
        if (!IsOwner) return; // Only update non-host clients

        currentHealth = newHealth;

        if (isDead)
        {
            hitDir = hit;
            Death(hitDir);
        }
    }

    public void Heal(float heal)
    {
        Debug.Log("Player heals: " + heal);
    }

    void Death(Vector3 hitPoint)
    {
        Debug.LogWarning("Player is dead");
        if (!IsOwner) return; // Only the owner disables their controls
        if (playerController != null) playerController.enabled = false;
        if (thirdPersonController != null) thirdPersonController.enabled = false;
      
        anim.enabled = false;
        Vector3 forceDirection = (hitPoint-transform.position).normalized; // Calculate the direction opposite to the hit point            
        rb.AddForce(forceDirection * 10, ForceMode.Impulse); // Apply force in the opposite direction        
        EnableRagdoll(forceDirection);
        
    }

}