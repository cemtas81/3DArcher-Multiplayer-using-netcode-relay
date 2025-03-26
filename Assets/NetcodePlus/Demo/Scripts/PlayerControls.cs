using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetcodePlus.Demo
{
    /// <summary>
    /// Keyboard controls manager
    /// </summary>

    public class PlayerControls : MonoBehaviour
    {
        [Header("Actions")]
        public KeyCode action_key = KeyCode.Space;
        public KeyCode attack_key = KeyCode.LeftShift;
        public KeyCode jump_key = KeyCode.LeftControl;

        [Header("Camera")]
        public KeyCode cam_rotate_left = KeyCode.Q;
        public KeyCode cam_rotate_right = KeyCode.E;

        [Header("Menu")]
        public KeyCode menu_accept = KeyCode.Return;
        public KeyCode menu_cancel = KeyCode.Backspace;
        public KeyCode menu_pause = KeyCode.Escape;

        private Vector2 move;
        private float rotate_cam;
        private float mouse_scroll = 0f;

        private bool press_action;
        private bool press_attack;
        private bool press_jump;

        private bool press_accept;
        private bool press_cancel;
        private bool press_pause;

        private static PlayerControls instance = null;

        void Awake()
        {
            if (instance == null)
                instance = this;
        }

        void Update()
        {
            move = Vector2.zero;
            rotate_cam = 0f;
            press_action = false;
            press_attack = false;
            press_jump = false;

            press_accept = false;
            press_cancel = false;

            Vector2 wasd = Vector2.zero;
            if (Input.GetKey(KeyCode.A))
                wasd += Vector2.left;
            if (Input.GetKey(KeyCode.D))
                wasd += Vector2.right;
            if (Input.GetKey(KeyCode.W))
                wasd += Vector2.up;
            if (Input.GetKey(KeyCode.S))
                wasd += Vector2.down;

            Vector2 arrows = Vector2.zero;
            if (Input.GetKey(KeyCode.LeftArrow))
                arrows += Vector2.left;
            if (Input.GetKey(KeyCode.RightArrow))
                arrows += Vector2.right;
            if (Input.GetKey(KeyCode.UpArrow))
                arrows += Vector2.up;
            if (Input.GetKey(KeyCode.DownArrow))
                arrows += Vector2.down;

            if (Input.GetKey(cam_rotate_left))
                rotate_cam += -1f;
            if (Input.GetKey(cam_rotate_right))
                rotate_cam += 1f;

            if (Input.GetKeyDown(action_key))
                press_action = true;
            if (Input.GetKeyDown(attack_key))
                press_attack = true;
            if (Input.GetKeyDown(jump_key))
                press_jump = true;

            if (Input.GetKeyDown(menu_accept))
                press_accept = true;
            if (Input.GetKeyDown(menu_cancel))
                press_cancel = true;
            if (Input.GetKeyDown(menu_pause))
                press_pause = true;

            move = (arrows + wasd);
            move = move.normalized * Mathf.Min(move.magnitude, 1f);
            mouse_scroll = Input.mouseScrollDelta.y;
        }

        public Vector2 GetMove() { return move; }
        public bool IsMoving() { return move.magnitude > 0.1f; }
        public float GetRotateCam() { return rotate_cam; }

        public bool IsPressAttack() { return press_attack; }
        public bool IsPressAction() { return press_action; }
        public bool IsPressJump() { return press_jump; }

        public bool IsPressMenuAccept() { return press_accept; }
        public bool IsPressMenuCancel() { return press_cancel; }
        public bool IsPressPause() { return press_pause; }

        public float GetMouseScroll()
        {
            return mouse_scroll;
        }

        public bool IsPressedByName(string name)
        {
            return Input.GetKeyDown(name);
        }

        public static PlayerControls Get()
        {
            return instance;
        }
    }

}