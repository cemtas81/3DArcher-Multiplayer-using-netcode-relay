using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    #region Properties
    private Camera cam;

    public Trajectory trajectory;
    [SerializeField] float PushForce = 4f;
    [SerializeField] private LayerMask BallLayer;
    private Ball defaultBall;

    bool isDragging;

    Vector3 startPoint;
    Vector3 endPoint;
    Vector3 direction;
    Vector3 force;
    float distance;

    #endregion

    private void Start()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Input.GetMouseButtonDown(0))
        {
            defaultBall = null;

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, BallLayer.value, QueryTriggerInteraction.Collide))
            {
                if (hit.collider != null)
                {
                    Time.timeScale = 0.1f;
                    isDragging = true;
                    defaultBall = hit.collider.gameObject.GetComponent<Ball>();
                    OnDragStart();
                }
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (defaultBall != null)
            {
                Time.timeScale = 1f;
                isDragging = false;
                OnDragEnd();
            }
        }
        if (isDragging)
        {
            OnDrag();
        }
    }

    #region Drag

    private void OnDragStart()
    {
        var mousePos = Input.mousePosition; 
        mousePos.z = 10; 

        startPoint = cam.ScreenToWorldPoint(mousePos);
        trajectory.Show();
    }

    private void OnDrag()
    {
        var mousePos = Input.mousePosition;
        mousePos.z = 10;

        endPoint = cam.ScreenToWorldPoint(mousePos);
        distance = Vector3.Distance(startPoint, endPoint);
        direction = (startPoint - endPoint).normalized;
        force = distance * direction * PushForce;

        trajectory.UpdateDots(defaultBall.pos, force);
    }

    private void OnDragEnd()
    {
        defaultBall.Push(force);
        trajectory.Hide();
    }

    #endregion
}
