/*********************************************************************************************************
 * The FakeGravityBody class should be place on any moveable object you want drawn to your world
 * *******************************************************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FakeGravityBody : MonoBehaviour {

    // inspector variables
    [SerializeField, Tooltip("Attractor object to be drawn to, if left blank first available world will be used")]
    private FakeGravity attractor;
    [SerializeField, Tooltip("Set object solid once settled")]
    private bool setSolid = false;
    
    // privates
    private Transform _objTransform;
    private Rigidbody _objRigidbody;
    
    // properties
    public FakeGravity Attractor { get { return attractor; } set { attractor = value; } }
    
    // Use this for initialization
	private void Start () {
        // set rigidbody
        _objRigidbody = GetComponent<Rigidbody>();
        _objRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        _objRigidbody.useGravity = false;
        _objTransform = transform;
        // get attractor if not provided
        if (attractor == null)
        {
            attractor = GameObject.FindGameObjectWithTag("World").GetComponent<FakeGravity>();
        }
	}
	
	// Update is called once per frame
	private void Update () {
        // return if kinematic
        if (_objRigidbody.isKinematic)
        {
            return;
        }
        // check if object sleeping yet
        if (setSolid)
        {
            ObjectResting();
        }
        // apply gravity to object
        if (attractor != null)
        {
            attractor.Attract(_objTransform);
        }
	}

    /// <summary>
    /// Check if rigidbody is sleeping
    /// </summary>
    private void ObjectResting()
    {
        if(gameObject.GetComponent<Rigidbody>().IsSleeping())
        {
            _objRigidbody.isKinematic = true;
        }
    }
}
