using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace NetcodePlus.Demo
{
    public class Box : SNetworkBehaviour
    {
        [Header("Sync")]
        public float sync_refresh_rate = 0.05f;
        public float sync_threshold = 0.1f;
        public float sync_interpolate = 5f;

        private SNetworkVariable<BoxSync> sync_state = new SNetworkVariable<BoxSync>();

        private Rigidbody rigid;
        private float owner_timer = 0f;

        private static List<Box> box_list = new List<Box>();

        protected override void Awake()
        {
            base.Awake();
            box_list.Add(this);
            rigid = GetComponent<Rigidbody>();
            sync_state.value.position = rigid.position;
            sync_state.value.rotation = rigid.rotation;
        }

        protected virtual void OnDestroy()
        {
            box_list.Remove(this);
        }

        protected override void OnSpawn()
        {
            sync_state.Init(this, "refresh", NetworkDelivery.UnreliableSequenced, NetworkActionTarget.All); //Send to all since we change owner of box
        }

        protected override void OnDespawn()
        {
            sync_state.Clear();
        }

        protected virtual void FixedUpdate()
        {
            UpdateOwner();
            UpdateNotOwner();
            UpdateServer();
        }

        private void UpdateOwner()
        {
            if (!IsOwner)
                return;

            //Auto refresh can usually be called in OnSpawn after Init(), but here we want to only auto refresh if the box moved
            bool should_refresh = sync_state.value.HasChanged(rigid.position, rigid.linearVelocity, rigid.rotation);
            sync_state.AutoRefresh(should_refresh, sync_refresh_rate);

            sync_state.value.position = rigid.position;
            sync_state.value.velocity = rigid.linearVelocity;
            sync_state.value.rotation = rigid.rotation;
        }

        private void UpdateNotOwner()
        {
            if (IsOwner)
                return;

            BoxSync state = sync_state.value;
            Vector3 offset = state.position - rigid.position; //Is the object position out of sync?

            if (offset.magnitude > sync_threshold)
                rigid.position = Vector3.MoveTowards(rigid.position, state.position, sync_interpolate * Time.fixedDeltaTime);

            if (offset.magnitude > sync_threshold * 10f)
                rigid.position = state.position; //Teleport if too far

            float angle = Quaternion.Angle(rigid.rotation, state.rotation);
            if (angle > sync_threshold)
                rigid.rotation = Quaternion.Slerp(rigid.rotation, state.rotation, sync_interpolate * Time.fixedDeltaTime);

            rigid.linearVelocity = Vector3.Lerp(rigid.linearVelocity, state.velocity, sync_interpolate * Time.fixedDeltaTime);
        }

        private void UpdateServer()
        {
            if (!IsServer)
                return;

            owner_timer += Time.fixedDeltaTime;
            if (owner_timer > sync_refresh_rate)
            {
                owner_timer = 0f;

                //Change owner to nearest player, for smoother pushing
                Explorer explorer = Explorer.GetNearest(transform.position, 5f);
                if (explorer != null)
                {
                    ClientData client = TheNetwork.Get().GetClientByPlayerID(explorer.PlayerID);
                    if (client != null && client.client_id != NetObject.OwnerId)
                    {
                        NetObject.ChangeOwner(client.client_id);
                    }
                }
            }
        }

        public static Box GetNearest(Vector3 pos, float range = 999f)
        {
            Box nearest = null;
            float min_dist = range;
            foreach (Box box in box_list)
            {
                float dist = (box.transform.position - pos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = box;
                }
            }
            return nearest;
        }

        public static List<Box> GetAll()
        {
            return box_list;
        }
    }

    [System.Serializable]
    public struct BoxSync : INetworkSerializable
    {
        public Vector3 position;
        public Vector3 velocity;
        public Quaternion rotation;

        public bool HasChanged(Vector3 new_pos, Vector3 new_velocity, Quaternion new_rotation)
        {
            return Vector3.Distance(position, new_pos) > 0.001f
                || Vector3.Distance(velocity, new_velocity) > 0.001f
                || Quaternion.Angle(rotation, new_rotation) > 0.001f;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref velocity);
            serializer.SerializeValue(ref rotation);
        }
    }
}
