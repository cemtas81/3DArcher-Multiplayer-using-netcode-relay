using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetcodePlus.Demo
{
    [CreateAssetMenu(fileName = "DemoData", menuName = "Netcode/DemoData", order = 0)]
    public class DemoData : ScriptableObject
    {
        public string[] names;

        public string GetRandomName()
        {
            return names[Random.Range(0, names.Length)];
        }

        public static DemoData Get()
        {
            return TheNetworkDemo.Get().data;
        }
    }
}
