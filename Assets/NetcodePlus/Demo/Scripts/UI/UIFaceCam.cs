using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetcodePlus.Demo
{

    public class UIFaceCam : MonoBehaviour
    {
        void Start()
        {

        }

        void Update()
        {
            CameraPlayer cam = CameraPlayer.Get();
            if (cam != null)
            {
                transform.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);
            }
        }
    }
}
