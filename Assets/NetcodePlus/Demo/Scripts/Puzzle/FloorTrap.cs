using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace NetcodePlus.Demo
{

    public class FloorTrap : SNetworkBehaviour
    {
        public float interval = 3f;
        public Transform spikes_mesh;
        public float spike_up_y;
        public float spike_down_y;

        private SNetworkActions actions;

        private float timer = 0f;
        private bool active = true;

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
            actions.Register("up", DoGoUp);
            actions.Register("down", DoGoDown);
        }

        protected override void OnDespawn()
        {
            actions.Clear();
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer > interval && interval > 0.01f)
            {
                if (active)
                    GoDown();
                else
                    GoUp();
            }

            //Visuals
            Vector3 tpos = spikes_mesh.localPosition;
            tpos.y = active ? spike_up_y : spike_down_y;
            float speed = active ? 40f : 5f;
            spikes_mesh.localPosition = Vector3.MoveTowards(spikes_mesh.localPosition, tpos, speed * Time.deltaTime);
        }

        public void GoUp()
        {
            actions?.Trigger("up");
        }

        public void GoDown()
        {
            actions?.Trigger("down");
        }

        private void DoGoUp()
        {
            active = true;
            timer = 0f;
        }

        private void DoGoDown()
        {
            active = false;
            timer = 0f;
        }

        private void OnTriggerStay(Collider other)
        {
            Explorer explorer = other.GetComponent<Explorer>();
            if (explorer != null && active)
            {
                explorer.Kill();
            }
        }
    }
}
