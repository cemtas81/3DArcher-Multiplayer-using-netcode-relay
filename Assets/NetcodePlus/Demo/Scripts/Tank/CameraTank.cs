using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetcodePlus.Demo
{
    public class CameraTank : MonoBehaviour
    {
        public Vector3 follow_offset;

        [Header("Rotate/Zoom")]
        public float rotate_speed = 120f; //Set a negative value to inverse rotation side
        public float zoom_speed = 0.5f;
        public float zoom_in_max = 0.5f;
        public float zoom_out_max = 1f;

        [Header("Smoothing")]
        public bool smooth_camera = false; //Camera will be more smooth but less accurate
        public float smooth_speed = 10f;
        public float smooth_rotate_speed = 90f;

        protected Camera cam;
        protected Transform target_transform;
        protected Transform cam_target_transform;
        protected Vector3 current_vel;
        protected float current_zoom = 0f;
        protected float add_rotate = 0f;

        protected Vector3 shake_vector = Vector3.zero;
        protected float shake_timer = 0f;
        protected float shake_intensity = 1f;

        protected static CameraTank _instance;

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

        protected virtual void LateUpdate()
        {
            SNetworkPlayer player = SNetworkPlayer.GetSelf();
            if (player != null)
            {
                UpdateCamera(player);
            }

            //Shake FX
            if (shake_timer > 0f)
            {
                shake_timer -= Time.deltaTime;
                shake_vector = new Vector3(Mathf.Cos(shake_timer * Mathf.PI * 8f) * 0.02f, Mathf.Sin(shake_timer * Mathf.PI * 7f) * 0.02f, 0f);
                transform.position += shake_vector * shake_intensity;
            }
        }

        protected virtual void UpdateControls()
        {
            /*PlayerControls controls = PlayerControls.Get();

            //Rotate
            add_rotate = 0f;
            add_rotate += controls.GetRotateCam() * rotate_speed;

            //Zoom 
            current_zoom += controls.GetMouseScroll() * zoom_speed; //Mouse scroll zoom
            current_zoom = Mathf.Clamp(current_zoom, -zoom_out_max, zoom_in_max);*/

        }

        protected virtual void UpdateCamera(SNetworkPlayer player)
        {
            //Rotate and Move
            //float rot = target_transform.rotation.eulerAngles.y + add_rotate * Time.deltaTime;
            float rot = player.transform.rotation.eulerAngles.y;
            Quaternion targ_rot = Quaternion.Euler(target_transform.rotation.eulerAngles.x, rot, 0f);

            if (smooth_camera)
            {
                target_transform.position = Vector3.SmoothDamp(target_transform.position, player.transform.position, ref current_vel, 1f / smooth_speed);
                target_transform.rotation = Quaternion.Slerp(target_transform.rotation, targ_rot, smooth_rotate_speed * Time.deltaTime);
            }
            else
            {
                target_transform.position = player.transform.position;
                target_transform.rotation = targ_rot;
            }

            //Zoom
            Vector3 targ_zoom = follow_offset * (1f - current_zoom);
            cam_target_transform.localPosition = Vector3.Lerp(cam_target_transform.localPosition, targ_zoom, 10f * Time.deltaTime);

            //Move to target position
            transform.rotation = cam_target_transform.rotation;
            transform.position = cam_target_transform.position;
        }
    }
}
