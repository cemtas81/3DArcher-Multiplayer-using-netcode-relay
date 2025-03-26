using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace NetcodePlus.Demo
{

    public class Lever : SNetworkBehaviour
    {
        public float refresh_rate = 0.5f;

        [Header("Visual")]
        public Transform lever_mesh;
        public float active_rot_x = 30f;
        public float inctive_rot_x = -30f;

        private SNetworkActions actions;
        private bool active = false;
        private float refresh_timer = 0f;

        protected override void Awake()
        {
            base.Awake();

        }

        protected virtual void OnDestroy()
        {

        }

        protected override void OnSpawn()
        {
            actions = new SNetworkActions(this);
            actions.RegisterInt("refresh", OnRefresh, NetworkDelivery.Unreliable);
        }

        protected override void OnDespawn()
        {
            actions.Clear();
        }

        protected void Update()
        {
            UpdateServer();

            Quaternion trot = active ? Quaternion.Euler(active_rot_x, 0f, 0f) : Quaternion.Euler(inctive_rot_x, 0f, 0f);
            lever_mesh.localRotation = Quaternion.Slerp(lever_mesh.localRotation, trot, 10f * Time.deltaTime);
        }

        private void UpdateServer()
        {
            if (IsServer)
            {
                refresh_timer += Time.deltaTime;
                if (refresh_timer > refresh_rate)
                {
                    refresh_timer = 0f;
                    Refresh();
                }
            }
        }

        private void Refresh()
        {
            if (IsServer)
            {
                actions?.Trigger("refresh", active ? 1 : 0);
            }
        }

        private void OnRefresh(int is_active)
        {
            if (!IsServer)
            {
                active = is_active > 0;
            }
        }

        public bool IsActive()
        {
            return active;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer)
                return;

            if (other.GetComponent<SNetworkPlayer>())
            {
                active = !active;
            }
        }
    }
}
