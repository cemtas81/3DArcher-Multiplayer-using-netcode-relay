using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetcodePlus.Demo
{
    public enum PowerupType
    {
        None = 0,
        Heal = 10,
        Speed = 20, //value in percentage
        Invulnerable = 30,
    }

    /// <summary>
    /// Powerups that can be taken by tanks
    /// </summary>

    public class Powerup : SNetworkBehaviour
    {
        public PowerupType type;
        public int value;
        public float duration;
        public float respawn_duration = 10f;

        [Header("Ref")]
        public GameObject mesh;

        private bool taken = false;
        private float timer = 0f;

        private SNetworkActions actions;

        protected override void OnBeforeSpawn()
        {
            //When a new client connects, send the current state of the powerup (taken or not) 
            SetSpawnData(taken ? 1 : 0);
        }

        protected override void OnSpawn()
        {
            actions = new SNetworkActions(this);
            actions.RegisterBehaviour("take", DoTake);
            actions.Register("respawn", DoRespawn);
            taken = GetSpawnDataInt32() > 0;
            mesh.SetActive(!taken);
        }

        protected override void OnDespawn()
        {
            actions.Clear();
        }

        private void Update()
        {
            if (IsServer && taken)
            {
                timer += Time.deltaTime;
                if (timer > respawn_duration)
                    Respawn();
            }
        }

        public void Take(Tank tank)
        {
            actions?.Trigger("take", tank); //DoTake(tank);
        }

        private void DoTake(SNetworkBehaviour stank)
        {
            if (!taken)
            {
                timer = 0f;
                taken = true;
                mesh.SetActive(!taken);
                Tank tank = stank.Get<Tank>();
                ApplyBonus(tank);
            }
        }

        private void ApplyBonus(Tank tank)
        {
            if (tank != null && !tank.IsGhost)
            {
                if (type == PowerupType.Heal)
                    tank.Heal(value);
                if (type == PowerupType.Speed)
                    tank.BoostSpeed(value / 100f, duration);
                if (type == PowerupType.Invulnerable)
                    tank.BoostInvulnerable(duration);
            }
        }

        public void Respawn()
        {
            actions?.Trigger("respawn"); //DoRespawn();
        }

        private void DoRespawn()
        {
            timer = 0f;
            taken = false;
            mesh.SetActive(!taken);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (taken)
                return;

            Tank tank = other.GetComponentInParent<Tank>();
            if (tank != null)
            {
                Take(tank);
            }
        }
    }
}
