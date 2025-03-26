using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


namespace NetcodePlus.Demo
{

    public class Explorer : SNetworkPlayer
    {
        [Header("Movement")]
        public float acceleration = 10f;
        public float move_speed = 2f;
        public float rotate_speed = 50f;
        public float gravity = 10f;
        public float ground_dist = 0.1f;

        [Header("Revive")]
        public float revive_dist = 5f;
        public float revive_duration = 5f;

        [Header("Ref")]
        public GameObject revive_bar_prefab;

        [Header("Sync")]
        public float sync_refresh_rate = 0.05f;
        public float sync_threshold = 0.1f;
        public float sync_interpolate = 5f;

        private Rigidbody rigid;
        private Animator animator;
        private Collider collide;
        private ItemAttach attach;
        private ProgressBar revive_bar;
        private Vector3 cmove;

        private SNetworkActions actions;
        private PlayerExplorerState sync_state = new PlayerExplorerState();
        private Vector3 moving;
        private Vector3 facing;
        private bool is_grounded = true;
        private bool is_dead = false;
        private float refresh_timer = 0f;
        private float revive_timer = 0f;

        private static List<Explorer> explorer_list = new List<Explorer>();

        protected override void Awake()
        {
            base.Awake();
            explorer_list.Add(this);
            rigid = GetComponent<Rigidbody>();
            animator = GetComponentInChildren<Animator>();
            collide = GetComponentInChildren<Collider>();
            attach = GetComponentInChildren<ItemAttach>();
            facing = transf.forward;
            sync_state.position = transf.position;
            sync_state.facing = transf.forward;

            if (revive_bar_prefab != null)
            {
                GameObject rev = Instantiate(revive_bar_prefab, transf);
                revive_bar = rev.GetComponent<ProgressBar>();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            explorer_list.Remove(this);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            actions = new SNetworkActions(this);
            actions.Register("kill", DoKill);
            actions.RegisterVector("revive", DoRevive);
            actions.RegisterSerializable("sync", ReceiveSync, NetworkDelivery.Unreliable);
        }

        protected override void OnDespawn()
        {
            base.OnDespawn();
            actions.Clear();
        }

        protected virtual void Update()
        {
            refresh_timer += Time.deltaTime;

            UpdateControls();
        }

        protected virtual void FixedUpdate()
        {
            UpdateOwner();
            UpdateNotOwner();
            UpdateRevive();
            UpdateGravity();
            UpdateAnims();
            UpdateServer();
        }

        private void UpdateControls()
        {
            if (!IsOwner)
                return;

            PlayerControls controls = PlayerControls.Get();
            Vector2 control_move = controls.GetMove();
            cmove = new Vector3(control_move.x, 0f, control_move.y);
            cmove = CameraPlayer.Get().GetFacingRotation() * cmove;
        }

        private void UpdateOwner()
        {
            if (!IsOwner)
                return;

            UpdateMove();

            if (refresh_timer < sync_refresh_rate)
                return;

            refresh_timer = 0f;
            sync_state.timing = sync_state.timing + 1;
            sync_state.position = transform.position;
            sync_state.moving = moving;
            sync_state.facing = facing;
            sync_state.control = cmove;
            sync_state.is_dead = is_dead;

            actions?.Trigger("sync", sync_state); // ReceiveSync(sync_state)
        }

        private void UpdateNotOwner()
        {
            if (IsOwner)
                return;

            Vector3 offset = sync_state.position - transform.position; //Is the character position out of sync?

            if (offset.magnitude > sync_threshold)
                transform.position = Vector3.MoveTowards(transform.position, sync_state.position, sync_interpolate * Time.fixedDeltaTime);

            if (offset.magnitude > sync_threshold * 10f)
                transform.position = sync_state.position; //Teleport if too far

            float angle = Vector3.Angle(transf.forward, sync_state.facing);
            if(angle > sync_threshold)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(sync_state.facing, Vector3.up), sync_interpolate * Time.fixedDeltaTime);

            moving = Vector3.Lerp(moving, sync_state.moving, 10f * Time.fixedDeltaTime);
            facing = Vector3.Lerp(facing, sync_state.facing, 10f * Time.fixedDeltaTime);
            rigid.linearVelocity = moving;

            if (sync_state.is_dead && !is_dead)
                DoKill();
            if (!sync_state.is_dead && is_dead)
                DoRevive(transf.position);
        }

        private void UpdateMove()
        {
            if (!is_dead)
            {
                //Find the direction the character should move
                Vector3 tmove = cmove * move_speed;

                //Apply the move calculated previously
                moving = Vector3.Lerp(moving, tmove, acceleration * Time.fixedDeltaTime);
                rigid.linearVelocity = moving;

                //Find facing direction
                if (IsMoving())
                    facing = new Vector3(moving.x, 0f, moving.z).normalized;

                //Apply the facing
                Quaternion targ_rot = Quaternion.LookRotation(facing, Vector3.up);
                rigid.MoveRotation(Quaternion.RotateTowards(rigid.rotation, targ_rot, rotate_speed * Time.fixedDeltaTime));

            }
        }

        private void UpdateGravity()
        {
            is_grounded = DetectGrounded();
            if (!is_grounded)
                rigid.linearVelocity = new Vector3(rigid.linearVelocity.x, -gravity, rigid.linearVelocity.z);
            else if(rigid.linearVelocity.y < 0f)
                rigid.linearVelocity = new Vector3(rigid.linearVelocity.x, 0f, rigid.linearVelocity.z);
        }

        private void UpdateRevive()
        {
            //Try Revive
            if (is_dead)
            {
                Explorer ally_near = null;
                foreach (Explorer explorer in explorer_list)
                {
                    if (explorer != this && !explorer.IsDead())
                    {
                        float dist = (explorer.transf.position - transf.position).magnitude;
                        if (dist < revive_dist)
                        {
                            ally_near = explorer;
                        }
                    }
                }

                if (ally_near)
                    revive_timer += Time.fixedDeltaTime;
                else if (revive_timer > 0f)
                    revive_timer -= Time.fixedDeltaTime;

                if (revive_timer > revive_duration)
                {
                    Vector3 dir = transf.position - ally_near.transf.position;
                    Vector3 pos = ally_near.transf.position + dir.normalized * 1f;
                    Revive(pos);
                    revive_timer = -1f;
                }
            }
        }

        private void UpdateAnims()
        {
            if (!IsSpawned)
                return;

            //Move anim
            animator.SetBool("move", IsMoving());

            //Revive Bar
            if (revive_bar != null)
            {
                bool show_bar = is_dead && revive_timer > 0.1f;
                if (revive_bar.gameObject.activeSelf != show_bar)
                    revive_bar.gameObject.SetActive(show_bar);
                revive_bar.transform.position = transf.position;
                revive_bar.SetValue(Mathf.RoundToInt(revive_timer * 100f));
                revive_bar.SetMax(Mathf.RoundToInt(revive_duration * 100f));
            }
        }

        private void UpdateServer()
        {
            if (!IsServer)
                return;

            //TryOpen door
            Key key = Key.Get(this);
            if (key != null)
            {
                Door door = Door.GetNearest(transf.position, 2f);
                if (door != null && door.key_can_open)
                    key.Use(door);
            }
        }

        private void ReceiveSync(SerializedData sdata)
        {
            if (IsOwner)
                return;

            PlayerExplorerState state = sdata.Get<PlayerExplorerState>();
            if (state.timing < sync_state.timing)
                return; //Old timing, ignore package, this means packages arrived in wrong order, prevent glitch

            sync_state = state;
        }

        public void Kill()
        {
            actions?.Trigger("kill");
        }

        private void DoKill()
        {
            if (!is_dead)
            {
                is_dead = true;
                revive_timer = 0f;
                moving = Vector3.zero;
                rigid.linearVelocity = Vector3.zero;
                animator.SetTrigger("death");
                collide.enabled = false;
                sync_state.is_dead = true;
                sync_state.timing += 10; //Avoid glitch, ignore next 10 refresh
            }
        }

        public void Revive(Vector3 pos)
        {
            actions?.Trigger("revive", pos);
        }

        private void DoRevive(Vector3 pos)
        {
            if (is_dead)
            {
                is_dead = false;
                moving = Vector3.zero;
                rigid.linearVelocity = Vector3.zero;
                transform.position = pos;
                sync_state.position = pos;
                animator.Rebind();
                collide.enabled = true;
                sync_state.is_dead = false;
                sync_state.timing += 10; //Avoid glitch, ignore next 10 refresh
            }
        }

        public void FaceTorward(Vector3 pos)
        {
            Vector3 face = (pos - transform.position);
            face.y = 0f;
            if (face.magnitude > 0.01f)
            {
                facing = face.normalized;
            }
        }

        private bool DetectGrounded()
        {
            float offset = 0.1f;
            return PhysicsTool.DetectGround(transf, transf.position + Vector3.up * offset, ground_dist + offset);
        }

        public Vector3 GetMove()
        {
            return moving;
        }

        public Vector3 GetFacing()
        {
            return facing;
        }

        public bool IsMoving()
        {
            Vector3 moveXZ = new Vector3(moving.x, 0f, moving.z);
            return moveXZ.magnitude > move_speed * 0.2f;
        }

        public bool HasKey()
        {
            return Key.Get(this) != null;
        }

        public Transform GetItemAttach()
        {
            if(attach != null)
                return attach.transform;
            return transf;
        }

        public bool IsDead()
        {
            return is_dead;
        }

        public static new Explorer Get(int player_id)
        {
            foreach (Explorer tank in explorer_list)
            {
                if (tank.player_id == player_id)
                    return tank;
            }
            return null;
        }

        public static new Explorer GetNearest(Vector3 pos, float range = 999f)
        {
            Explorer nearest = null;
            float min_dist = range;
            foreach (Explorer player in player_list)
            {
                float dist = (player.transform.position - pos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = player;
                }
            }
            return nearest;
        }

        public static new Explorer GetSelf()
        {
            return Get(TheNetwork.Get().PlayerID);
        }

        public static new List<Explorer> GetAll()
        {
            return explorer_list;
        }
    }

    [System.Serializable]
    public struct PlayerExplorerState : INetworkSerializable
    {
        public ulong timing; //Increased by 1 each frame
        public Vector3 position;
        public Vector3 moving;
        public Vector3 facing;
        public Vector2 control;
        public bool is_dead;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref timing);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref moving);
            serializer.SerializeValue(ref facing);
            serializer.SerializeValue(ref control);
            serializer.SerializeValue(ref is_dead);
        }
    }
}
