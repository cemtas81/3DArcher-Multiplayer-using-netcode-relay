using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NetcodePlus.Demo
{

    public class LobbyMatchmakingPanel : UIPanel
    {
        public Text players_count;

        private static LobbyMatchmakingPanel instance;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        protected override void Start()
        {
            base.Start();
            ClientLobby.Get().onMatchmaking += OnReceiveMatchmake;
        }

        protected override void Update()
        {
            base.Update();

            if (IsVisible())
            {
                if (!ClientLobby.Get().IsConnected())
                    Hide(); //Connection to lobby was lost
            }
        }

        private void OnReceiveMatchmake(LobbyGame game)
        {
            if (game != null && IsVisible())
            {
                //Display players currently found
                players_count.text = game.players_found + "/" + game.players_max;

                //If game_id is 0, matchmaking is still ongoing
                if (game.IsValid())
                {
                    MenuLobby.Get().ConnectToGame(game);
                }
            }
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            players_count.text = "";
        }

        public void OnClickCancel()
        {
            ClientLobby.Get().CancelMatchmaking();
            Hide();
        }

        public static LobbyMatchmakingPanel Get()
        {
            return instance;
        }
    }
}
