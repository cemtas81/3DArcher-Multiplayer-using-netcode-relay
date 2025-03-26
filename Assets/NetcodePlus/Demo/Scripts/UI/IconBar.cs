using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NetcodePlus.Demo
{

    public class IconBar : MonoBehaviour
    {
        public int value = 0;
        public int max_value = 4;
        public bool auto_refresh = true;

        public Image[] icons;
        public Sprite sprite_full;
        public Sprite sprite_empty;
        public Color color_full;
        public Color color_empty;

        void Awake()
        {

        }

        void Update()
        {
            if (auto_refresh)
                Refresh();
        }

        public void Refresh()
        {
            int index = 0;
            foreach (Image icon in icons)
            {
                icon.gameObject.SetActive(index < max_value);
                if (sprite_full != null)
                    icon.sprite = (index < value) ? sprite_full : sprite_empty;
                icon.color = (index < value) ? color_full : color_empty;
                index++;
            }
        }

        public void SetMat(Material mat)
        {
            foreach (Image icon in icons)
            {
                icon.material = mat;
            }
        }

        public void SetVisible(bool visible)
        {
            if (visible != gameObject.activeSelf)
                gameObject.SetActive(visible);
        }
    }
}
