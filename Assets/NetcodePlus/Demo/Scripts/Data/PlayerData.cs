using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetcodePlus.Demo
{
    [System.Serializable]
    public class PlayerData
    {
        public int player_id;
        public string username;
        public string character;
        public bool connected;
        public int score;

        public PlayerData(int pid) { player_id = pid; username = ""; character = ""; }
        public bool IsAssigned() { return !string.IsNullOrEmpty(username); }
    }
}
