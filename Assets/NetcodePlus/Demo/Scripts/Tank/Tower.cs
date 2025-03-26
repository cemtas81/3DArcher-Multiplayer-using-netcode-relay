using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace NetcodePlus.Demo
{

    public class Tower : SNetworkBehaviour
    {
        public MeshRenderer[] flags;
        public GameObject[] mesh_damage;
        public int hp_max = 5;

        private int player_id;
        private int hp;
        private bool active = false;

        private SNetworkActions actions;

        private static List<Tower> tower_list = new List<Tower>();

        protected override void Awake()
        {
            base.Awake();
            tower_list.Add(this);
            hp = hp_max;
        }

        protected virtual void OnDestroy()
        {
            tower_list.Remove(this);
        }

        protected override void OnReady()
        {
            actions = new SNetworkActions(this);
            actions.Register("damage", DoDamage);
            actions.RegisterSerializable("refresh", DoRefresh);
        }

        protected override void OnBeforeSpawn()
        {
            //Assign Tower ID
            PlayerSpawn nearest = PlayerSpawn.GetNearest(transform.position);
            player_id = nearest.player_id;

            TowerRefreshData sdata = new TowerRefreshData();
            sdata.player_id = player_id;
            sdata.hp = hp;

            PlayerData player = GameData.Get().GetPlayer(player_id);
            sdata.color = player != null ? player.character : "";
            active = player != null && player.connected;

            SetSpawnData(sdata);
        }

        protected override void OnSpawn()
        {
            TowerRefreshData sdata = GetSpawnData<TowerRefreshData>();
            player_id = sdata.player_id;
            hp = sdata.hp;
            UpdateColor(sdata.color);
        }

        void Update()
        {
            
        }

        public void Damage()
        {
            if(IsServer)
                actions?.Trigger("damage"); //DoDamage();
        }

        private void DoDamage()
        {
            if (active)
            {
                hp -= 1;
                UpdateMesh();
                Refresh();

                if (hp <= 0)
                    Kill();
            }
        }

        public void Kill()
        {
            if (IsServer && !NetObject.IsDestroyed)
            {
                NetObject.Destroy();
            }
        }

        public void Refresh()
        {
            if (!IsServer || !IsSpawned)
                return;

            //Get color
            PlayerData player = GameData.Get().GetPlayer(player_id);
            if (player != null && player.connected)
            {
                TowerRefreshData refresh = new TowerRefreshData();
                refresh.player_id = player_id;
                refresh.hp = hp;
                refresh.color = player.character;
                active = true;

                actions?.Trigger("refresh", refresh); //DoRefresh
            }
        }

        private void DoRefresh(SerializedData sdata)
        {
            TowerRefreshData refresh = sdata.Get<TowerRefreshData>();
            hp = refresh.hp;
            active = true;
            UpdateColor(refresh.color);
            UpdateMesh();
        }

        private void UpdateColor(string color)
        {
            PlayerChoiceData choice = PlayerChoiceData.Get(GameMode.Tank, color);
            if (choice != null)
            {
                foreach (MeshRenderer flag in flags)
                    flag.material.color = choice.color;
            }
        }

        private void UpdateMesh()
        {
            int damage = hp_max - hp;
            if (damage >= 0 && damage < mesh_damage.Length)
            {
                GameObject valid = mesh_damage[damage];
                foreach (GameObject msh in mesh_damage)
                {
                    bool active = (msh == valid);
                    if (active != msh.activeSelf)
                        msh.SetActive(active);
                }
            }
        }

        public int PlayerID { get { return player_id; } }
        public int HP { get { return hp; } }

        public static Tower Get(int player_id)
        {
            foreach (Tower tower in tower_list)
            {
                if (tower.player_id == player_id)
                    return tower;
            }
            return null;
        }

        public static List<Tower> GetAll()
        {
            return tower_list;
        }
    }

    [System.Serializable]
    public class TowerRefreshData : INetworkSerializable
    {
        public int player_id;
        public int hp;
        public string color;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref player_id);
            serializer.SerializeValue(ref hp);
            serializer.SerializeValue(ref color);
        }
    }
}
