using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


namespace NetcodePlus.Demo
{

    public class Tank : SNetworkPlayer
    {
        [Header("Movement")]
        public float acceleration = 10f;
        public float move_speed = 2f;
        public float rotate_speed = 50f;
        public float gravity = 10f;
        public float ground_dist = 0.25f;

        [Header("Attack")]
        public float attack_cooldown = 1f;
        public GameObject attack_bullet;
        public Transform shoot_root;

        [Header("Life")]
        public int hp = 5;
        public float revive_duration = 5f;
        public float invulnerable_duration = 3f;

        [Header("Ref")]
        public GameObject mesh;
        public GameObject speed_bonus;
        public GameObject shield_bonus;

        [Header("Sync")]
        public float sync_refresh_rate = 0.05f;
        public float sync_threshold = 0.1f;
        public float sync_interpolate = 5f;

        private Rigidbody rigid;
        private Animator animator;
        private Vector2 cmove;

        private SNetworkActions actions;
        private PlayerTankState sync_state = new PlayerTankState();
        private bool is_grounded = true;
        private int hp_max;
        private float attack_timer = 0f;
        private float refresh_timer = 0f;
        private float revive_timer = 0f;
        private float speed_mult = 1f;
        private float speed_timer = 0f;
        private float invul_timer = 0f;
        private bool ghost = false;
        private bool destroyed = false;

        private static List<Tank> tank_list = new List<Tank>();

        protected override void Awake()
        {
            base.Awake();
            tank_list.Add(this);
            rigid = GetComponent<Rigidbody>();
            animator = GetComponentInChildren<Animator>();
            speed_bonus?.SetActive(false);
            shield_bonus?.SetActive(false);
            sync_state.position = transf.position;
            sync_state.facing = transf.forward;
            attack_timer = attack_cooldown;
            hp_max = hp;

            Vector3 face = Vector3.zero - transf.position;
            transf.rotation = Quaternion.LookRotation(face.normalized, Vector3.up);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            tank_list.Remove(this);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            actions = new SNetworkActions(this);
            actions.RegisterVector("shoot", DoShoot);
            actions.RegisterInt("damage", DoDamage);
            actions.RegisterInt("heal", DoHeal);
            actions.Register("kill", DoKill);
            actions.RegisterVector("revive", DoRevive);
            actions.RegisterSerializable("speed", DoBoostSpeed);
            actions.RegisterFloat("invulnerable", DoInvulnerable);
            actions.RegisterSerializable("sync", ReceiveSync, NetworkDelivery.Unreliable);
        }

        protected override void OnDespawn()
        {
            base.OnDespawn();
            actions.Clear();
        }

        protected virtual void Update()
        {
            attack_timer += Time.deltaTime * speed_mult;
            refresh_timer += Time.deltaTime;
            revive_timer += Time.deltaTime;
            speed_timer -= Time.deltaTime;
            invul_timer -= Time.deltaTime;

            UpdateControls();
        }

        protected virtual void FixedUpdate()
        {
            UpdateOwner();
            UpdateNotOwner();
            UpdateGravity();
            UpdateAll();
            UpdateServer();
        }

        private void UpdateControls()
        {
            if (!IsOwner)
                return;

            PlayerControls controls = PlayerControls.Get();
            cmove = controls.GetMove();

            if (controls.IsPressAction() && !ghost)
                Shoot();
        }

        private void UpdateOwner()
        {
            if (!IsOwner)
                return;

            float move = cmove.y * move_speed;
            float rotate = cmove.x * rotate_speed;
            Move(move);
            Rotate(rotate);

            if (speed_timer < 0f)
                speed_mult = 1f;

            if (refresh_timer < sync_refresh_rate)
                return;

            refresh_timer = 0f;
            sync_state.timing = sync_state.timing + 1;
            sync_state.hp = hp;
            sync_state.position = transform.position;
            sync_state.facing = transform.forward;
            sync_state.control = cmove;
            sync_state.speed_mult = speed_mult;
            sync_state.ghost = ghost;

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

            float angle = Vector3.Angle(transf.forward, sync_state.facing);
            if(angle > sync_threshold)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(sync_state.facing, Vector3.up), sync_interpolate * Time.deltaTime);

            float move = sync_state.control.y * move_speed;
            float rotate = sync_state.control.x * rotate_speed;
            Move(move);
            Rotate(rotate);
        }

        private void UpdateGravity()
        {
            is_grounded = DetectGrounded();
            if (!is_grounded)
                rigid.linearVelocity = new Vector3(rigid.linearVelocity.x, -gravity, rigid.linearVelocity.z);
            else if(rigid.linearVelocity.y < 0f)
                rigid.linearVelocity = new Vector3(rigid.linearVelocity.x, 0f, rigid.linearVelocity.z);
        }

        private void UpdateAll()
        {
            if (!IsSpawned)
                return;

            bool boosted = IsBoosted();
            if(speed_bonus != null && speed_bonus.activeSelf != boosted)
                speed_bonus.SetActive(boosted);

            bool invuln = IsInvulnerable();
            if (shield_bonus != null && shield_bonus.activeSelf != invuln)
                shield_bonus.SetActive(invuln);

            if (!CanRevive())
            {
                if (hp > 1)
                    hp = 1;

                if (ghost && !destroyed)
                {
                    destroyed = true;
                    hp = 0;
                }
            }
        }

        private void UpdateServer()
        {
            if (!IsServer)
                return;

            //Revive if in ghost mode
            if (ghost && !destroyed)
            {
                if (revive_timer > revive_duration)
                {
                    Revive();
                }
            }
        }

        private void ReceiveSync(SerializedData sdata)
        {
            if (IsOwner)
                return;

            PlayerTankState state = sdata.Get<PlayerTankState>();
            if (state.timing < sync_state.timing)
                return; //Old timing, ignore package, this means packages arrived in wrong order, prevent glitch

            sync_state = state;
            hp = state.hp;
            speed_mult = state.speed_mult;
            SetGhost(state.ghost);
        }

        public void Move(float val)
        {
            Vector3 forward = transf.forward;
            forward.y = 0f;
            rigid.linearVelocity = Vector3.MoveTowards(rigid.linearVelocity, forward * val, acceleration * speed_mult * Time.fixedDeltaTime);
        }

        public void Rotate(float val)
        {
            Vector3 trot = new Vector3(0f, val * Time.fixedDeltaTime, 0f);
            rigid.angularVelocity = Vector3.MoveTowards(rigid.angularVelocity, trot, acceleration * speed_mult * Time.fixedDeltaTime);
        }

        public void Shoot()
        {
            if (IsOwner && attack_timer > attack_cooldown)
            {
                actions?.Trigger("shoot", transf.forward); //DoShoot()
            }
        }

        private void DoShoot(Vector3 dir)
        {
            if (attack_timer > attack_cooldown)
            {
                attack_timer = 0f;
                animator.SetTrigger("shoot");
                GameObject bobj = Instantiate(attack_bullet, shoot_root.position, Quaternion.identity);
                TankBullet bullet = bobj.GetComponent<TankBullet>();
                bullet.direction = dir;
                bullet.player_id = player_id;
            }
        }

        public void Damage(int damage)
        {
            if(IsServer)
                actions?.Trigger("damage", damage); //DoDamage();
        }

        private void DoDamage(int damage)
        {
            if (IsInvulnerable())
                return;

            hp -= damage;
            revive_timer = 0f;
            SetGhost(true);
            sync_state.timing += 10; //Avoid glitch, ignore next 10 refresh

            if (hp <= 0)
                Kill();
        }

        public void Heal(int heal)
        {
            if (IsServer)
                actions?.Trigger("heal", heal); //DoHeal();
        }

        private void DoHeal(int heal)
        {
            if (CanRevive())
            {
                hp += heal;
            }
        }

        public void Kill()
        {
            if (IsServer)
                actions?.Trigger("kill"); //DoKill()
        }

        private void DoKill()
        {
            SetGhost(true);
            destroyed = true;
            revive_timer = 0f;
            sync_state.timing += 10; //Avoid glitch, ignore next 10 refresh

            Tower tower = Tower.Get(player_id);
            tower?.Kill(); //Also kill tower
        }

        public void Revive()
        {
            if (IsServer)
            {
                PlayerSpawn spawn = PlayerSpawn.Get(player_id);
                Vector3 pos = spawn.transform.position;
                actions?.Trigger("revive", pos); //DoRevive()
            }
        }

        private void DoRevive(Vector3 pos)
        {
            if (CanRevive())
            {
                transf.position = pos;
                Vector3 face = Vector3.zero - transf.position;
                transf.rotation = Quaternion.LookRotation(face.normalized, Vector3.up);
                sync_state.position = pos;
                sync_state.facing = face.normalized;
                sync_state.timing += 10; //Avoid glitch, ignore next 10 refresh
                SetGhost(false);
                revive_timer = 0f;
                invul_timer = invulnerable_duration;
            }
        }

        public void BoostSpeed(float percent, float duration)
        {
            TankBoostValue val = new TankBoostValue();
            val.percent = percent;
            val.duration = duration;
            actions?.Trigger("speed", val);
        }

        public void BoostInvulnerable(float duration)
        {
            actions?.Trigger("invulnerable", duration);
        }

        private void DoInvulnerable(float duration)
        {
            if (!ghost)
            {
                invul_timer = duration;
            }
        }

        private void DoBoostSpeed(SerializedData sdata)
        {
            if (!ghost)
            {
                TankBoostValue val = sdata.Get<TankBoostValue>();
                speed_mult = 1f + val.percent;
                speed_timer = val.duration;
                sync_state.speed_mult = speed_mult;
            }
        }

        private void SetGhost(bool aghost)
        {
            if (aghost != ghost)
            {
                ghost = aghost;
                sync_state.ghost = aghost;
                mesh.SetActive(!aghost);
            }
        }

        public bool CanRevive()
        {
            Tower tower = Tower.Get(player_id);
            return tower != null;
        }

        private bool DetectGrounded()
        {
            float offset = 0.1f;
            return PhysicsTool.DetectGround(transf, transf.position + Vector3.up * offset, ground_dist + offset);
        }

        public bool IsBoosted()
        {
            return speed_mult > 1.01f;
        }

        public bool IsInvulnerable()
        {
            return invul_timer > 0f;
        }

        public int HP { get { return hp; } }
        public int HPMax { get { return hp_max; } }
        public bool IsGhost { get { return ghost; } }

        public static new Tank Get(int player_id)
        {
            foreach (Tank tank in tank_list)
            {
                if (tank.player_id == player_id)
                    return tank;
            }
            return null;
        }

        public static new List<Tank> GetAll()
        {
            return tank_list;
        }
    }

    [System.Serializable]
    public struct PlayerTankState : INetworkSerializable
    {
        public ulong timing; //Increased by 1 each frame
        public int hp;
        public Vector3 position;
        public Vector3 facing;
        public Vector2 control;
        public float speed_mult;
        public bool ghost;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref timing);
            serializer.SerializeValue(ref hp);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref facing);
            serializer.SerializeValue(ref control);
            serializer.SerializeValue(ref speed_mult);
            serializer.SerializeValue(ref ghost);
        }
    }

    [System.Serializable]
    public struct TankBoostValue : INetworkSerializable
    {
        public float percent;
        public float duration;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref percent);
            serializer.SerializeValue(ref duration);
        }
    }
}
