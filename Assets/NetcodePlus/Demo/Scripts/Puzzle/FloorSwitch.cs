using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace NetcodePlus.Demo
{

    public class FloorSwitch : SNetworkBehaviour
    {
        public float detect_range = 1f;
        public float refresh_rate = 0.5f;

        [Header("Visual")]
        public Transform button_mesh;
        public float active_y = 0f;
        public float inctive_y = -0.1f;

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

            Vector3 tpos = button_mesh.localPosition;
            tpos.y = active ? active_y : inctive_y;
            button_mesh.localPosition = Vector3.MoveTowards(button_mesh.localPosition, tpos, 2f * Time.deltaTime);
        }

        private void UpdateServer()
        {
            if (IsServer)
            {
                SNetworkPlayer player = SNetworkPlayer.GetNearest(transform.position, detect_range);
                active = (player != null);

                Box box = Box.GetNearest(transform.position, detect_range);
                if (box != null)
                    active = true;

                refresh_timer += Time.deltaTime;
                if (refresh_timer > refresh_rate)
                {
                    refresh_timer = 0f;
                    Refresh();
                }
            }
        }

        private void SlowUpdate()
        {
            if (IsServer)
            {
                Refresh();
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
    }
}
