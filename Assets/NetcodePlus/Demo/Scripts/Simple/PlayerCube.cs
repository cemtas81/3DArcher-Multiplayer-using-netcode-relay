using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace NetcodePlus.Demo
{
    public class PlayerCube : SNetworkPlayer
    {
        public float move_speed = 2f;
        public float sync_refresh_rate = 0.05f;
        public float sync_threshold = 0.1f;
        public float sync_interpolate = 5f;

        public PlayerCubeMat[] player_mats;

        private Rigidbody rigid;
        private MeshRenderer render;
        private SNetworkActions actions;
        private PlayerCubeState sync_state = new PlayerCubeState();
        private float refresh_timer = 0f;

        protected override void Awake()
        {
            base.Awake();
            rigid = GetComponent<Rigidbody>();
            render = GetComponentInChildren<MeshRenderer>();
            sync_state.position = transform.position;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            actions = new SNetworkActions(this);
            actions.RegisterSerializable("sync", ReceiveSync, NetworkDelivery.Unreliable);

            InitMaterial();
        }

        protected override void OnDespawn()
        {
            base.OnDespawn();
            actions.Clear();
        }

        protected virtual void Update()
        {
            UpdateOwner();
            UpdateNotOwner();
        }

        private void InitMaterial()
        {
            //Default value
            if (player_id >= 0 && player_id < player_mats.Length)
                render.material = player_mats[player_id].mat;

            //Selected material value
            GameData gdata = GameData.Get();
            PlayerData pdata = gdata?.GetPlayer(player_id);
            if (pdata != null)
            {
                foreach (PlayerCubeMat mat in player_mats)
                {
                    if (mat.color == pdata.character)
                        render.material = mat.mat;
                }
            }
        }

        private void UpdateOwner()
        {
            if (!IsOwner)
                return;

            //Get Key Controls
            PlayerControls controls = PlayerControls.Get();
            Vector2 cmove = controls.GetMove();
            Vector3 move = new Vector3(cmove.x, 0f, cmove.y) * move_speed;

            //Rotate controls to camera angle
            CameraPlayer cam = CameraPlayer.Get();
            if(cam != null)
                move = cam.GetFacingRotation() * move;

            //Move
            Move(move);

            //Refresh Timer
            refresh_timer += Time.deltaTime;
            if (refresh_timer < sync_refresh_rate)
                return;

            //Refresh to other clients
            refresh_timer = 0f;
            sync_state.timing = sync_state.timing + 1;
            sync_state.position = transform.position;
            sync_state.move = move;

            actions?.Trigger("sync", sync_state); // ReceiveSync(sync_state)
        }

        private void UpdateNotOwner()
        {
            if (IsOwner)
                return;

            Vector3 offset = sync_state.position - transform.position; //Is the character position out of sync?

            if (offset.magnitude > sync_threshold)
                transform.position = Vector3.MoveTowards(transform.position, sync_state.position, sync_interpolate * Time.deltaTime);

            if (offset.magnitude > sync_threshold * 10f)
                transform.position = sync_state.position; //Teleport if too far

            Move(sync_state.move);
        }

        private void Move(Vector3 dir)
        {
            rigid.linearVelocity = dir;
        }

        private void ReceiveSync(SerializedData sdata)
        {
            if (IsOwner)
                return;

            PlayerCubeState state = sdata.Get<PlayerCubeState>();
            if (state.timing < sync_state.timing)
                return; //Old timing, ignore package, this means packages arrived in wrong order, prevent glitch

            sync_state = state;
        }
    }

    [System.Serializable]
    public struct PlayerCubeState : INetworkSerializable
    {
        public ulong timing; //Increased by 1 each frame
        public Vector3 position;
        public Vector3 move;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref timing);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref move);
        }
    }

    [System.Serializable]
    public struct PlayerCubeMat
    {
        public string color;
        public Material mat;
    }
}
