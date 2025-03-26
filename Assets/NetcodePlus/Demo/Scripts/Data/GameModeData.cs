using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetcodePlus.Demo
{
    public enum GameMode
    {
        None = 0,
        Simple = 10,
        Tank = 20,
        Puzzle = 30,

    }
    [CreateAssetMenu(fileName = "GameMode", menuName = "Data/GameMode", order = 10)]
    public class GameModeData : ScriptableObject
    {
        public GameMode mode;
        public string scene;
        public int players_max = 4;

        private static List<GameModeData> modes = new List<GameModeData>();

        public static void Load(string folder = "/")
        {
            modes.Clear();
            modes.AddRange(Resources.LoadAll<GameModeData>(folder));
        }

        public static GameModeData GetByScene(string scene)
        {
            foreach (GameModeData choice in modes)
            {
                if (choice.scene == scene)
                    return choice;
            }
            return null;
        }

        public static GameModeData Get(GameMode mode)
        {
            foreach (GameModeData choice in modes)
            {
                if (choice.mode == mode)
                    return choice;
            }
            return null;
        }

        public static List<GameModeData> GetAll()
        {
            return modes;
        }
    }
}
