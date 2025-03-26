using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NetcodePlus.Demo
{
    public class GameOverPanel : UIPanel
    {
        public Text title;
        public Text subtitle;

        private static GameOverPanel _instance;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
        }

        protected override void Start()
        {
            base.Start();

        }

        protected override void Update()
        {
            base.Update();

        }

        public void Show(string title, string subtitle)
        {
            if (this.title != null)
                this.title.text = title;
            if (this.subtitle != null)
                this.subtitle.text = subtitle;
            Show();
        }

        public void OnClickQuit()
        {
            StartCoroutine(QuitRoutine());
        }

        private IEnumerator QuitRoutine()
        {
            BlackPanel.Get().Show();

            yield return new WaitForSeconds(1f);

            Menu.GoToLastMenu();
        }

        public static GameOverPanel Get()
        {
            return _instance;
        }
    }

}
