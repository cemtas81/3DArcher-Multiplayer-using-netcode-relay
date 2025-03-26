using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetcodePlus.Demo
{

    public class FXTool : MonoBehaviour
    {
        public static void FX(GameObject prefab, Vector3 pos, float duration = 4f)
        {
            if (prefab != null)
            {
                GameObject fx = Instantiate(prefab, pos, prefab.transform.rotation);
                Destroy(fx, duration);
            }
        }
    }
}