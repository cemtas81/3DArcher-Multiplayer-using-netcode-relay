using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace NetcodePlus.Demo
{
    [System.Serializable]
    public class GameData
    {
        public GameMode mode;
        public PlayerData[] players;

        private static GameData data = null;

        public PlayerData AddNewPlayer(string username)
        {
            for (int i = 0; i < players.Length; i++)
            {
                PlayerData player = players[i];
                if (!player.IsAssigned())
                {
                    player.username = username;
                    return player;
                }
            }
            return null;
        }

        public PlayerData GetPlayer(int player_id)
        {
            foreach (PlayerData player in players)
            {
                if (player != null && player.player_id == player_id)
                    return player;
            }
            return null;
        }

        public PlayerData GetPlayer(string username)
        {
            foreach (PlayerData player in players)
            {
                if (player != null && player.username == username)
                    return player;
            }
            return null;
        }

        public PlayerData GetPlayerByCharacter(string character)
        {
            foreach (PlayerData player in players)
            {
                if (player != null && player.character == character)
                    return player;
            }
            return null;
        }

        public int CountConnected()
        {
            int count = 0;
            foreach (PlayerData player in players)
            {
                if (player != null && player.player_id >= 0 && player.connected)
                    count += 1;
            }
            return count;
        }

        public static void Unload()
        {
            data = null;
        }
        
        public static void Override(GameData dat)
        {
            data = dat;
        }

        public static GameData Create(GameMode mode, int nb_players)
        {
            data = new GameData();
            data.mode = mode;
            data.players = new PlayerData[nb_players];
            for (int i = 0; i < nb_players; i++)
                data.players[i] = new PlayerData(i);
            return data;
        }

        public static GameData Get()
        {
            return data;
        }
    }
}
