using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace NetcodePlus.Demo
{

    public class Arrow : SNetworkBehaviour
    {
        public float speed = 10f;
        public float duration = 10f;

        private float timer = 0f;

        protected override void OnSpawn()
        {
            base.OnSpawn();
        }

        private void Update()
        {
            transform.position += transform.forward * speed * Time.deltaTime;

            timer += Time.deltaTime;
            if (timer > duration)
                NetObject.Destroy();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer)
                return;

            if (other.isTrigger)
                return;

            Explorer explorer = other.GetComponent<Explorer>();
            if (explorer != null)
            {
                explorer.Kill();
            }

            if (timer > 0.1f)
                NetObject.Destroy();
        }

        public class ArrowSync : INetworkSerializable
        {
            public Vector3 dir;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref dir);
            }
        }
    }
}