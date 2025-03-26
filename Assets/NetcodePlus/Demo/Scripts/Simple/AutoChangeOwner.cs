using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace NetcodePlus.Demo
{
    public class AutoChangeOwner : SNetworkBehaviour
    {
        public float refresh_rate = 0.2f;

        private float owner_timer = 0f;

        protected override void Awake()
        {
            base.Awake();

        }

        protected void Update()
        {
            UpdateServer();
        }

        private void UpdateServer()
        {
            if (!IsServer)
                return;

            owner_timer += Time.fixedDeltaTime;
            if (owner_timer > refresh_rate)
            {
                owner_timer = 0f;

                //Change owner to nearest player, for smoother pushing
                SNetworkPlayer player = SNetworkPlayer.GetNearest(transform.position, 5f);
                if (player != null)
                {
                    ClientData client = TheNetwork.Get().GetClientByPlayerID(player.PlayerID);
                    if (client != null && client.client_id != NetObject.OwnerId)
                    {
                        NetObject.ChangeOwner(client.client_id);
                    }
                }
            }
        }

    }
}
