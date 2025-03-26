using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetcodePlus.Demo
{
    public class LobbyMatchSelectPanel : UIPanel
    {
        public OptionSelectorInt mode_selector;

        private static LobbyMatchSelectPanel instance;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        public void OnClickOK()
        {
            GameMode mode = (GameMode)mode_selector.GetSelectedValue();
            GameModeData mdata = GameModeData.Get(mode);
            DemoConnectData ddata = new DemoConnectData(mode);
            ClientLobby.Get().SetConnectionExtraData(ddata);
            
            //Use Game Mode as group so that players of different game mode dont get matched
            ClientLobby.Get().StartMatchmaking(mode.ToString(), mdata.scene, mdata.players_max);
            LobbyMatchmakingPanel.Get().Show();
            Hide();
        }

        public void OnClickBack()
        {
            Hide();
        }

        public static LobbyMatchSelectPanel Get()
        {
            return instance;
        }
    }
}
