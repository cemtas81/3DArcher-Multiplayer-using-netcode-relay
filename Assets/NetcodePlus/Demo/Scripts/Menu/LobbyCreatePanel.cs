using NetcodePlus;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NetcodePlus.Demo
{
    public class LobbyCreatePanel : UIPanel
    {
        public InputField title_field;
        public OptionSelectorInt mode_field;
        public Toggle toggle_new;
        public Toggle toggle_load;

        private bool creating = false;
        private float timer = 0f;

        private static LobbyCreatePanel instance;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        protected override void Update()
        {
            base.Update();

            if (creating && IsVisible())
            {
                if (timer > 5f || Input.GetKeyDown(KeyCode.Escape))
                {
                    creating = false;
                    ConnectingPanel.Get().Hide();
                }
            }
        }

        protected override void Start()
        {
            base.Start();

            ClientLobby client = ClientLobby.Get();
            client.onConnect += OnConnect;
            client.onRefresh += ReceiveRefresh;
        }

        private void OnConnect(bool success)
        {
            title_field.text = ClientLobby.Get().Username + "'s Game";
        }

        public void OnClickCreate()
        {
            GameMode mode = (GameMode)mode_field.GetSelectedValue();
            GameModeData mdata = GameModeData.Get(mode);
            string scene = mdata.scene;

            if (!SceneNav.DoSceneExist(scene))
                return; //Scene invalid
            if (title_field.text.Length == 0)
                return; //Title/save invaild

            creating = true;
            timer = 0f;
            LobbyPanel.Get().WaitForCreate();
            Hide();

            string save = NetworkTool.GenerateRandomID();
            GameData.Unload(); //Unload previous data

            CreateGameData cdata = ClientLobby.Get().GetCreateData(title_field.text, save, scene);
            cdata.extra = NetworkTool.SerializeInt32((int)mode);
            MenuLobby.Get().CreateGame(cdata);
        }

        private void ReceiveRefresh(LobbyGame room)
        {
            if (creating && room.HasPlayer(ClientLobby.Get().UserID))
            {
                Hide();
                creating = false;
                ConnectingPanel.Get().Hide();
                LobbyRoomPanel.Get().ShowGame(room);
            }
        }

        public void OnClickBack()
        {
            LobbyPanel.Get().Show();
            Hide();
        }


        public static LobbyCreatePanel Get()
        {
            return instance;
        }
    }
}