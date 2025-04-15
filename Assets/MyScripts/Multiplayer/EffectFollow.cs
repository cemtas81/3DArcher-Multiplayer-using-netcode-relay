using UnityEngine;

public class EffectFollow : MonoBehaviour
{
   
    public void Follow(Transform target)
    {
        if (target != null)
        {
            transform.position = target.position;
            transform.rotation = target.rotation;
        }
    }
}
