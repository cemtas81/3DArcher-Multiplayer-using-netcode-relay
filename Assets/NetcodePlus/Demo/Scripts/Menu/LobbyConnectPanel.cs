using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NetcodePlus;
using System.Threading.Tasks;

namespace NetcodePlus.Demo
{
    public class LobbyConnectPanel : UIPanel
    {
        public InputField username_input;
        public InputField password_input;
        public OptionSelector character_select;
        public Text error_txt;

        private string character;

        private static LobbyConnectPanel instance;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        protected override void Update()
        {
            base.Update();

        }

        protected override void Start()
        {
            base.Start();

            LoadUser();
        }

        private void SaveUser()
        {
            Menu.username = username_input.text;
        }

        private void LoadUser()
        {
            string name = Menu.username;
            if (name == null && DemoData.Get() != null)
                name = DemoData.Get().GetRandomName();
            if (name == null)
                name = "player";

            username_input.text = name;
        }

        public void OnClickConnect()
        {
            if (!SaveTool.IsValidFilename(username_input.text))
                return;

            SaveUser();

            string user = username_input.text;
            string pass = password_input != null ? password_input.text : "";
            character = character_select.GetSelectedValue();
            ConnectToLobby(user, pass);
        }

        private async void ConnectToLobby(string user, string pass)
        {
            ConnectingPanel.Get().Show();
            error_txt.text = "";

            bool success = await Authenticator.Get().Login(user, pass);

            if (!success)
            {
                error_txt.text = Authenticator.Get().GetError();
                ConnectingPanel.Get().Hide();
                return; //Failed to connect
            }

            Debug.Log("Authentication Succeed: " + user);

            bool connected = await ClientLobby.Get().Connect();

            ConnectingPanel.Get().Hide();
            if (connected)
            {
                MenuLobby.Get().HideAllPanels();
                LobbyPanel.Get().Show();
            }
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            error_txt.text = "";
        }

        public string GetCharacter()
        {
            return character;
        }

        public static LobbyConnectPanel Get()
        {
            return instance;
        }
    }
}