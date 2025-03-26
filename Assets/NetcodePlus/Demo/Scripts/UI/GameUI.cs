using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace NetcodePlus.Demo
{

    public class GameUI : MonoBehaviour
    {
        private float timer = 0f;

        void Start()
        {

        }

        private void Update()
        {
            //Disconnect panel
            timer += Time.deltaTime;
            if (!TheNetwork.Get().IsConnected() && !GameOverPanel.Get().IsVisible() && timer > 5f)
            {
                GameOverPanel.Get().Show("Diconnected", "Connection to server was lost");
            }
        }

        public void OnClickQuit()
        {
            TheNetwork.Get().Disconnect();
            Menu.GoToLastMenu();
        }
    }
}
