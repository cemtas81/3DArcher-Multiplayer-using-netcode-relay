using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace NetcodePlus.Demo
{
    public class Door : SNetworkBehaviour
    {
        public FloorSwitch[] switches;
        public Lever[] levers;
        public int trigger_count = 1;
        public bool key_can_open = false;
        public bool permanent = false;
        public bool reverse = false;
        public float refresh_rate = 0.5f;

        [Header("Visual")]
        public Transform door_left;
        public Transform door_right;
        public float door_open_dist = 0.5f;

        private SNetworkActions actions;
        private bool opened = false;
        private bool force_open = false;
        private float refresh_timer = 0f;

        private static List<Door> door_list = new List<Door>();

        protected override void Awake()
        {
            base.Awake();
            door_list.Add(this);
        }

        protected virtual void OnDestroy()
        {
            door_list.Remove(this);
        }

        protected override void OnSpawn()
        {
            actions = new SNetworkActions(this);
            actions.RegisterSerializable("refresh", OnRefresh);
        }

        protected override void OnDespawn()
        {
            actions.Clear();
        }

        private void Update()
        {
            UpdateServer();

            Vector3 ltpos = opened ? new Vector3(-door_open_dist, 0f, 0f) : Vector3.zero;
            Vector3 rtpos = opened ? new Vector3(door_open_dist, 0f, 0f) : Vector3.zero;
            door_left.localPosition = Vector3.MoveTowards(door_left.localPosition, ltpos, 1f * Time.deltaTime);
            door_right.localPosition = Vector3.MoveTowards(door_right.localPosition, rtpos, 1f * Time.deltaTime);
        }

        private void UpdateServer()
        {
            if (IsServer)
            {
                opened = CountTriggers() >= trigger_count;
                opened = reverse ? !opened : opened;

                if (force_open)
                    opened = true;

                if (permanent && opened)
                    force_open = true;

                refresh_timer += Time.deltaTime;
                if (refresh_timer > refresh_rate)
                {
                    refresh_timer = 0f;
                    Refresh();
                }
            }
        }

        private int CountTriggers()
        {
            int count = 0;
            foreach (FloorSwitch swit in switches)
            {
                if (swit.IsActive())
                    count++;
            }

            foreach (Lever lever in levers)
            {
                if (lever.IsActive())
                    count++;
            }

            return count;
        }

        public void Open()
        {
            force_open = true;
            opened = true;
        }

        public void Close()
        {
            force_open = false;
            opened = false;
        }

        private void Refresh()
        {
            if (IsServer)
            {
                DoorState state = new DoorState();
                state.opened = opened;
                state.force = force_open;
                actions?.Trigger("refresh", state);
            }
        }

        private void OnRefresh(SerializedData sdata)
        {
            if (!IsServer)
            {
                DoorState state = sdata.Get<DoorState>();
                opened = state.opened;
                force_open = state.force;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            Explorer player = collision.collider.GetComponent<Explorer>();
            if (key_can_open && player != null)
            {
                Key key = Key.Get(player);
                if (key != null)
                    key.Use(this);
            }
        }

        public static Door GetNearest(Vector3 pos, float range = 999f)
        {
            Door nearest = null;
            float min_dist = range;
            foreach (Door door in door_list)
            {
                float dist = (door.transform.position - pos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = door;
                }
            }
            return nearest;
        }

        public static List<Door> GetAll()
        {
            return door_list;
        }
    }

    public struct DoorState : INetworkSerializable
    {
        public bool opened;
        public bool force;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref opened);
            serializer.SerializeValue(ref force);
        }
    }
}
