using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetcodePlus.Demo
{
    /// <summary>
    /// Main camera script
    /// </summary>

    public class CameraPlayer : MonoBehaviour
    {
        [Header("Rotate/Zoom")]
        public float rotate_speed = 150f; //Set a negative value to inverse rotation side
        public float zoom_speed = 0.5f;
        public float zoom_in_max = 0.5f;
        public float zoom_out_max = 1f;

        [Header("Smoothing")]
        public bool smooth_camera = false; //Camera will be more smooth but less accurate
        public float smooth_speed = 10f;
        public float smooth_rotate_speed = 90f;

        [Header("Target")]
        public GameObject follow_target;
        public Vector3 follow_offset;

        protected Vector3 current_vel;
        protected float current_zoom = 0f;
        protected float add_rotate = 0f;

        protected Transform target_transform;
        protected Transform cam_target_transform;

        protected Camera cam;

        protected static CameraPlayer _instance;

        protected virtual void Awake()
        {
            _instance = this;
            cam = GetComponent<Camera>();

            GameObject cam_target = new GameObject("CameraTarget");
            target_transform = cam_target.transform;
            target_transform.position = transform.position - follow_offset;

            GameObject cam_target_cam = new GameObject("CameraTargetCam");
            cam_target_transform = cam_target_cam.transform;
            cam_target_transform.SetParent(target_transform);
            cam_target_transform.localPosition = follow_offset;
            cam_target_transform.localRotation = transform.localRotation;
        }

        protected virtual void Start()
        {
            
        }

        protected virtual void LateUpdate()
        {
            if (follow_target == null)
            {
                //Auto assign follow target
                SNetworkPlayer first = SNetworkPlayer.GetSelf();
                if (first != null)
                    follow_target = first.gameObject;
                return;
            }

            UpdateControls();
            UpdateCamera();
        }

        protected virtual void UpdateControls()
        {
            PlayerControls controls = PlayerControls.Get();

            //Rotate
            add_rotate = 0f;
            add_rotate += controls.GetRotateCam() * rotate_speed;

            //Zoom 
            current_zoom += controls.GetMouseScroll() * zoom_speed; //Mouse scroll zoom
            current_zoom = Mathf.Clamp(current_zoom, -zoom_out_max, zoom_in_max);
        }

        protected virtual void UpdateCamera()
        {
            //Rotate and Move
            float rot = target_transform.rotation.eulerAngles.y + add_rotate * Time.deltaTime;
            Quaternion targ_rot = Quaternion.Euler(target_transform.rotation.eulerAngles.x, rot, 0f);

            if (smooth_camera)
            {
                target_transform.position = Vector3.SmoothDamp(target_transform.position, follow_target.transform.position, ref current_vel, 1f / smooth_speed);
                target_transform.rotation = Quaternion.Slerp(target_transform.rotation, targ_rot, smooth_rotate_speed * Time.deltaTime);
            }
            else
            {
                target_transform.position = follow_target.transform.position;
                target_transform.rotation = targ_rot;
            }

            //Zoom
            Vector3 targ_zoom = follow_offset * (1f - current_zoom);
            cam_target_transform.localPosition = Vector3.Lerp(cam_target_transform.localPosition, targ_zoom, 10f * Time.deltaTime);

            //Move to target position
            transform.rotation = cam_target_transform.rotation;
            transform.position = cam_target_transform.position;
        }

        public virtual void MoveToTarget(Vector3 target)
        {
            Vector3 diff = target - target_transform.position;
            target_transform.position = target;
            transform.position += diff;
        }

        public virtual bool IsInside(Vector2 screen_pos)
        {
            return cam.pixelRect.Contains(screen_pos);
        }

        public virtual Quaternion GetFacingRotation()
        {
            Vector3 facing = GetFacingFront();
            return Quaternion.LookRotation(facing.normalized, Vector3.up);
        }

        public Vector3 GetTargetPos()
        {
            return target_transform.position;
        }

        public Quaternion GetTargetRotation()
        {
            return target_transform.rotation;
        }

        public Vector3 GetTargetPosOffsetFace(float dist)
        {
            return target_transform.position + GetFacingFront() * dist;
        }

        public Vector3 GetFacingFront()
        {
            Vector3 dir = transform.forward;
            dir.y = 0f;
            return dir.normalized;
        }

        public Vector3 GetFacingRight()
        {
            Vector3 dir = transform.right;
            dir.y = 0f;
            return dir.normalized;
        }

        public Vector3 GetFacingDir()
        {
            return transform.forward;
        }

        public Camera GetCam()
        {
            return cam;
        }

        public static Camera GetCamera()
        {
            Camera camera = _instance != null ? _instance.GetCam() : Camera.main;
            return camera;
        }

        public static CameraPlayer Get()
        {
            return _instance;
        }
    }

}