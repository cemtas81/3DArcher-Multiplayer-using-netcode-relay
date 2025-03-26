using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetcodePlus.Demo
{

    public class PuzzleGame : SMonoBehaviour
    {
        private SNetworkActions actions;
        private bool ended = false;

        private static PuzzleGame instance;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        protected void Start()
        {
            BlackPanel.Get().Show(true);

            if (!TheNetwork.Get().IsActive())
            {
                //Start in test mode, when running directly from Unity Scene
                Authenticator.Get().LoginTest("Player"); //May not work with more advanced auth system, works in Test mode
                DemoConnectData cdata = new DemoConnectData(GameMode.Puzzle);
                TheNetwork.Get().SetConnectionExtraData(cdata);
                TheNetwork.Get().StartHost(NetworkData.Get().game_port);
            }
        }

        protected override void OnReady()
        {
            actions = new SNetworkActions(null); //Can only have one null NetworkActionHandler in the entire scene
            actions.Register("win", DoWin);
            actions.Register("lose", DoLose);
            BlackPanel.Get().Hide();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            actions?.Clear();
        }

        private void Update()
        {
            int count = 0;
            int dead_count = 0;

            foreach (Explorer explorer in Explorer.GetAll())
            {
                count++;
                if (explorer.IsDead())
                    dead_count++;
            }

            if (count > 0 && dead_count >= count)
                LoseGame();
        }

        public void WinGame()
        {
            actions?.Trigger("win");
        }

        public void LoseGame()
        {
            actions?.Trigger("lose");
        }

        private void DoWin()
        {
            if (!ended)
            {
                ended = true;
                TimeTool.WaitFor(1f, () =>
                {
                    GameOverPanel.Get().Show("Victory", "You found the treasure!");
                });
            }
        }

        private void DoLose()
        {
            if (!ended)
            {
                ended = true;
                TimeTool.WaitFor(1f, () =>
                {
                    GameOverPanel.Get().Show("Defeat", "Everyone died!");
                });
            }
        }

        public static PuzzleGame Get()
        {
            return instance;
        }
    }
}
