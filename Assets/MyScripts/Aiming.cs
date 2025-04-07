using UnityEngine;
using UnityEngine.InputSystem;

public class Aiming : MonoBehaviour
{
    public Transform target; 
    public Transform aim;
    public void Aim()
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad == null)
        {

            Vector3 pos = target.position - transform.position;
            pos.y = 0;
            aim.position = pos;
        }

        else if (gamepad.rightStick.IsActuated(0F))
        {

            Vector3 positionPoint = aim.position - transform.position;

            positionPoint.y = 0;
           
            target.position = positionPoint;

        }
    }
}
