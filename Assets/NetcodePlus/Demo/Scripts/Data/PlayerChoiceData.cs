using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetcodePlus.Demo
{
    [CreateAssetMenu(fileName = "PlayerChoice", menuName = "Data/PlayerChoice", order=10)]
    public class PlayerChoiceData : ScriptableObject
    {
        public GameMode mode;
        public string id;
        public GameObject prefab;
        public Color color;

        private static List<PlayerChoiceData> choices = new List<PlayerChoiceData>();

        public static void Load(string folder = "/")
        {
            choices.Clear();
            choices.AddRange(Resources.LoadAll<PlayerChoiceData>(folder));
            foreach (PlayerChoiceData choice in choices)
                TheNetwork.Get().RegisterPrefab(choice.prefab);
        }

        public static PlayerChoiceData Get(GameMode mode)
        {
            foreach (PlayerChoiceData choice in choices)
            {
                if (choice.mode == mode)
                    return choice;
            }
            return null;
        }

        public static PlayerChoiceData Get(string id)
        {
            foreach (PlayerChoiceData choice in choices)
            {
                if (choice.id == id)
                    return choice;
            }
            return null;
        }

        public static PlayerChoiceData Get(GameMode mode, string id)
        {
            foreach (PlayerChoiceData choice in choices)
            {
                if (choice.mode == mode && choice.id == id)
                    return choice;
            }
            return null;
        }

        public static List<PlayerChoiceData> GetAll(GameMode mode)
        {
            List<PlayerChoiceData> vchoices = new List<PlayerChoiceData>();
            foreach (PlayerChoiceData choice in choices)
            {
                if (choice.mode == mode)
                    vchoices.Add(choice);
            }
            return vchoices;
        }

        public static List<PlayerChoiceData> GetAll()
        {
            return choices;
        }
    }
}
