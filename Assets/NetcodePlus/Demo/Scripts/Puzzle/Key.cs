using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetcodePlus.Demo
{

    public class Key : SNetworkBehaviour
    {

        private SNetworkActions actions;
        private Explorer bearer = null;

        private static List<Key> key_list = new List<Key>();
 

        protected override void Awake()
        {
            base.Awake();
            key_list.Add(this);
        }

        protected virtual void OnDestroy()
        {
            key_list.Remove(this);
        }

        protected override void OnSpawn()
        {
            actions = new SNetworkActions(this);
            actions.RegisterBehaviour("take", DoTake);
            actions.RegisterBehaviour("use", DoUse);
            actions.Register("drop", DoDrop);
        }

        protected override void OnDespawn()
        {

        }

        private void Update()
        {
            if (bearer != null)
            {
                Transform attach = bearer.GetItemAttach();
                transform.position = attach.position;
                transform.rotation = attach.rotation;
            }
        }

        public void Take(Explorer player)
        {
            if(bearer == null && !player.HasKey())
                actions?.Trigger("take", player);
        }

        private void DoTake(SNetworkBehaviour splayer)
        {
            if (bearer == null)
            {
                Explorer player = splayer.Get<Explorer>();
                if(player != null && !player.HasKey())
                    bearer = player;
            }
        }

        public void Drop()
        {
            if (bearer != null)
                actions?.Trigger("drop");
        }

        private void DoDrop()
        {
            if (bearer != null)
            {
                transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
                transform.rotation = Quaternion.identity;
                bearer = null;
            }
        }

        public void Use(Door door)
        {
            actions?.Trigger("use", door);
        }

        private void DoUse(SNetworkBehaviour sdoor)
        {
            Door door = sdoor.Get<Door>();
            door.Open();
            NetObject.Destroy();
        }

        public bool IsTaken()
        {
            return bearer != null;
        }

        public Explorer GetBearer()
        {
            return bearer;
        }

        private void OnTriggerEnter(Collider other)
        {
            Explorer explorer = other.GetComponent<Explorer>();
            if (explorer != null)
            {
                Take(explorer);
            }
        }

        public static Key Get(Explorer explorer)
        {
            foreach (Key key in key_list)
            {
                if (key.bearer == explorer)
                    return key;
            }
            return null;
        }
    }
}
