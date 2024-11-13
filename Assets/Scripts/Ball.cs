using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    [HideInInspector] public Rigidbody rb;

    [HideInInspector] public Vector3 pos { get { return transform.position; } }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Push(Vector3 force)
    {
        rb.AddForce(force, ForceMode.Impulse);
    }

}
