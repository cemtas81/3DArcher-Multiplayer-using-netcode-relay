using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetcodePlus.Demo
{

    public class Hazard : MonoBehaviour
    {
        protected void Awake()
        {
            

        }

        private void OnTriggerEnter(Collider other)
        {
            Explorer explorer = other.GetComponent<Explorer>();
            if (explorer != null)
            {
                explorer.Kill();
            }
        }
    }
}
