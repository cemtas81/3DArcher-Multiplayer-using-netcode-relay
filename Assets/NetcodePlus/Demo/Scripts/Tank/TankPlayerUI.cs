using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NetcodePlus.Demo
{

    public class TankPlayerUI : MonoBehaviour
    {
        public int player_id;
        public Text username;
        public IconBar hp_bar;

        void Start()
        {
            hp_bar.gameObject.SetActive(false);
            username.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!TheNetwork.Get().IsConnected())
                return;

            GameData gdata = GameData.Get();
            if (gdata == null)
                return;

            PlayerData player = gdata.GetPlayer(player_id);
            if (player == null)
                return;

            username.text = player.username;

            bool visible = player.connected;
            if (hp_bar.gameObject.activeSelf != visible)
                hp_bar.gameObject.SetActive(visible);
            if (username.gameObject.activeSelf != visible)
                username.gameObject.SetActive(visible);

            PlayerChoiceData choice = PlayerChoiceData.Get(GameMode.Tank, player.character);
            if (choice != null)
                hp_bar.color_full = choice.color;

            Tank tank = Tank.Get(player_id);
            if (tank != null)
            {
                hp_bar.value = tank.HP;
                hp_bar.max_value = tank.HPMax;
            }
            else
            {
                hp_bar.value = 0;
            }
        }
    }
}
