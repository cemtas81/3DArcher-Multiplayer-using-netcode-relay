using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetcodePlus.Demo
{
    public class ArrowSpawn : MonoBehaviour
    {
        public GameObject arrow_prefab;
        public float spawn_interval = 2f;
        public float spawn_offset = 0f;
        public Transform spawn_root;

        private float spawn_timer = 0f;

        void Start()
        {
            TheNetwork.Get().RegisterPrefab(arrow_prefab);
            spawn_timer = -spawn_offset;
        }

        void Update()
        {
            if (TheNetwork.Get().IsServer)
            {
                spawn_timer += Time.deltaTime;
                if (spawn_timer > spawn_interval)
                {
                    spawn_timer = 0f;
                    SpawnArrow();
                }
            }
        }

        public void SpawnArrow()
        {
            GameObject arr = Instantiate(arrow_prefab, spawn_root.position, spawn_root.rotation);
            Arrow arrow = arr.GetComponent<Arrow>();
            arrow.NetObject.Spawn();
        }
    }
}
