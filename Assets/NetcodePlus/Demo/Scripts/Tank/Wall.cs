using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetcodePlus.Demo
{

    public class Wall : SNetworkBehaviour
    {
        public int hp_max = 3;
        public GameObject[] mesh_damage;

        private SNetworkActions actions;
        private int hp = 3;

        protected override void Awake()
        {
            base.Awake();
            hp = hp_max;
        }

        protected override void OnSpawn()
        {
            actions = new SNetworkActions(this);
            actions.Register("damage", DoDamage);
        }

        public void Damage()
        {
            if(IsServer)
                actions?.Trigger("damage");
        }

        private void DoDamage()
        {
            hp -= 1;
            UpdateMesh();
            if (hp <= 0)
                Kill();
        }

        public void Kill()
        {
            if(IsServer)
                NetObject.Destroy();
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
    }
}
