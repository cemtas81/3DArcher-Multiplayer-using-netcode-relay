using EasyTextEffects.Editor.MyBoxCopy.Extensions;
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
    private Animator anim;
    private NetworkVariable<HealthUpdate> healthVariable = new NetworkVariable<HealthUpdate>(new HealthUpdate { Health = 100, dead = false },
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private Rigidbody rb;
    private List<ISeekable> seekableList = new List<ISeekable>();
    ThirdPersonController thirdPersonController;
    private Rigidbody[] _ragdollRigidbodies;
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
        Debug.Log($"[{NetworkManager.Singleton.LocalClientId}] Health updated: {newValue.Health}, Dead: {newValue.dead}");

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
        _ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        DisableRagdoll();
    }
    private void DisableRagdoll()
    {
       rb.WakeUp();
        foreach (var rigidbody in _ragdollRigidbodies)
        {
            rigidbody.isKinematic = true;
        }
        anim.enabled = true;
        if (playerController != null) playerController.enabled = true;
        if (thirdPersonController != null) thirdPersonController.enabled = true;
    }
    private void EnableRagdoll()
    {
        rb.IsSleeping();
        foreach (var rigidbody in _ragdollRigidbodies)
        {
            rigidbody.isKinematic = false;
        }
        anim.enabled = false;
        if (playerController != null) playerController.enabled = false;
        if (thirdPersonController != null) thirdPersonController.enabled = false;
    }
    public void Damage(float damage)
    {
        DamageServerRpc(damage);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DamageServerRpc(float damage)
    {
        float newHealth = healthVariable.Value.Health - damage;
        healthVariable.Value = new HealthUpdate
        {
            Health = Mathf.Max(0, newHealth),
            dead = newHealth <= 0
        };

        if (newHealth <= 0)
        {
            Death();
        }

        // Ensure clients update their UI/logic
        ApplyDamageClientRpc(newHealth, healthVariable.Value.dead);
    }

    [ClientRpc]
    private void ApplyDamageClientRpc(float newHealth, bool isDead)
    {
        if (!IsOwner) return; // Only update non-host clients

        currentHealth = newHealth;

        if (isDead)
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
        if (!IsOwner) return; // Only the owner disables their controls
        if (playerController != null) playerController.enabled = false;
        if (thirdPersonController != null) thirdPersonController.enabled = false;
        EnableRagdoll();
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