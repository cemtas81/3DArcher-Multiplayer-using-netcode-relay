using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;

namespace NetcodePlus.Demo
{
    /// <summary>
    /// Extension to TheNetwork, specific to this demo, should be on the same object that is DontDestroyOnLoad
    /// </summary>

    public class TheNetworkDemo : MonoBehaviour
    {
        public DemoData data;

        private static TheNetworkDemo instance;

        private void Awake()
        {
            instance = this;
            GameModeData.Load();
            PlayerChoiceData.Load();
        }

        private void Start()
        {
            TheNetwork network = TheNetwork.Get();
            network.onConnect += OnConnect;
            network.checkApproval += OnApprove;
            network.onSendWorld += OnSendWorld;
            network.onReceiveWorld += OnReceiveWorld;
            network.findPlayerID += FindPlayerID;
            network.findPlayerPrefab += FindPlayerPrefab;
            network.findPlayerSpawnPos += FindPlayerPos;
            network.onClientQuit += OnDisconnect;

            if (network.IsConnected())
                OnConnect(); //Run now if already connected
        }

        void OnDestroy()
        {
            TheNetwork network = TheNetwork.Get();
            network.onConnect -= OnConnect;
            network.checkApproval -= OnApprove;
            network.onSendWorld -= OnSendWorld;
            network.onReceiveWorld -= OnReceiveWorld;
            network.findPlayerID -= FindPlayerID;
            network.findPlayerPrefab -= FindPlayerPrefab;
            network.findPlayerSpawnPos -= FindPlayerPos;
            network.onClientQuit -= OnDisconnect;
        }

        //Step 1, connect server
        //Use this to initialize the game on the server
        private void OnConnect()
        {
            if (TheNetwork.Get().IsServer)
            {
                if (TheNetwork.Get().ServerType == ServerType.DedicatedServer)
                {
                    //Start based on scene selected
                    LobbyGame game = TheNetwork.Get().GetLobbyGame();
                    GameModeData mdata = GameModeData.GetByScene(game.scene);
                    if (mdata != null)
                        GameData.Create(mdata.mode, mdata.players_max);
                    else
                        GameData.Create(GameMode.Simple, mdata.players_max);
                }
                else
                {
                    //Start based on connection data
                    ConnectionData connect = TheNetwork.Get().GetConnectionData();
                    DemoConnectData ddata = NetworkTool.NetDeserialize<DemoConnectData>(connect.extra);
                    GameModeData mdata = GameModeData.Get(ddata.mode);
                    int players_max = mdata != null ? mdata.players_max : NetworkData.Get().players_max;
                    GameData.Create(ddata.mode, players_max);
                }
            }
        }

        //Step 2, approve or not connecting clients
        //Optional, if not defined will just return true
        private bool OnApprove(ulong client_id, ConnectionData cdata)
        {
            GameData gdata = GameData.Get();
            if (gdata == null)
                return false; //Game not found

            //Find player id by username (for reconnections)
            PlayerData user_player = gdata.GetPlayer(cdata.username);
            if (user_player != null && TheNetwork.Get().GetClientByPlayerID(user_player.player_id) == null)
                return true; //Already in data

            int count = gdata.CountConnected();
            return count < NetworkData.Get().players_max;
        }

        //Step 3, assign player ID
        //Optional, if not defined the player_id will be the same than client_id,
        //but this means it will not be possible to reconnect to same game with previous data since netcode assign a new client_id each connection
        private int FindPlayerID(ulong client_id)
        {
            GameData gdata = GameData.Get();
            ClientData client = TheNetwork.Get().GetClient(client_id);
            if (client == null || gdata == null)
                return -1; //Client not or game not found

            DemoConnectData cdata = client.GetExtraData<DemoConnectData>();

            //Find player id by username (for reconnections)
            PlayerData user_player = gdata.GetPlayer(client.username);
            if (user_player != null && user_player.player_id >= 0 && TheNetwork.Get().GetClientByPlayerID(user_player.player_id) == null)
            {
                user_player.connected = true;
                return user_player.player_id; //Return only if no other user already connected with this username
            }

            //No player found, assign new player ID
            PlayerData player = gdata.AddNewPlayer(client.username);
            if (player != null)
            {
                player.connected = true;
                player.character = AssignColor(cdata.character);
                return player.player_id;
            }

            //Max player count reached
            return -1;
        }

        private string AssignColor(string preferred)
        {
            GameMode mode = GameData.Get().mode;
            PlayerData pcolor = GameData.Get().GetPlayerByCharacter(preferred);
            PlayerChoiceData choice = PlayerChoiceData.Get(mode, preferred);

            if (pcolor == null && choice != null)
            {
                return choice.id; //Color not taken, use preferred color
            }

            List<string> colors = new List<string>();
            foreach (PlayerChoiceData achoice in PlayerChoiceData.GetAll(mode))
            {
                colors.Add(achoice.id);
            }
            
            foreach (PlayerData player in GameData.Get().players)
            {
                if(player != null)
                    colors.Remove(player.character);
            }

            if (colors.Count > 0)
                return colors[Random.Range(0, colors.Count)];
            return ""; //No valid colors
        }

        //Step 4-A, server send GameData to client
        private void OnSendWorld(FastBufferWriter writer)
        {
            //Not using writer.WriteNetworkSerializable like everywhere else,
            //because a small change in the data structure of the save (like if loading an old save) would crash everything
            //instead, using NetworkTool.Serialize allow to be more flexible, and uses the same serialization as when saving to disk
            GameData sdata = GameData.Get();
            if (sdata != null)
            {
                byte[] bytes = NetworkTool.Serialize(sdata);
                writer.WriteValueSafe(sdata.mode);
                writer.WriteValueSafe(bytes.Length);
                if (bytes.Length > 0)
                    writer.WriteBytesSafe(bytes, bytes.Length);
            }
            else
            {
                writer.WriteValueSafe(0);
            }
        }

        //Step 4-B, client receives GameData
        private void OnReceiveWorld(FastBufferReader reader)
        {
            //Not using reader.ReadNetworkSerializable like everywhere else,
            //because a small change in the data structure of the save (like if loading an old save) would crash everything
            //instead, using NetworkTool.Deserialize allow to be more flexible, and uses the same serialization as when saving to disk
            reader.ReadValueSafe(out GameMode mode);
            if (mode != GameMode.None)
            {
                reader.ReadValueSafe(out int count);
                byte[] bytes = new byte[count];
                reader.ReadBytesSafe(ref bytes, count);
                GameData sdata = NetworkTool.Deserialize<GameData>(bytes);
                if (sdata != null)
                {
                    GameData.Override(sdata);
                }
            }
        }

        //Step 5, select player character
        //Optional, if this isnt defined, it will use the default_player prefab assigned in Resources/NetworkData
        private GameObject FindPlayerPrefab(int player_id)
        {
            //Different for each game mode
            GameData gdata = GameData.Get();
            PlayerData pdata = gdata.GetPlayer(player_id);
            if (pdata != null)
            {
                //Specific color
                PlayerChoiceData cdata = PlayerChoiceData.Get(gdata.mode, pdata.character);
                if (cdata != null)
                    return cdata.prefab;
            }

            return NetworkData.Get().player_default;
        }

        //Step 6, select player starting position
        //Optional, if not defined, will spawn at the PlayerSpawn with the same player_id (or at Vector3.zero if none)
        private Vector3 FindPlayerPos(int player_id)
        {
            PlayerSpawn spawn = PlayerSpawn.Get(player_id);
            if (spawn != null)
                return spawn.GetRandomPosition();
            return Vector3.zero;
        }

        //Set our custom flag connected to false when a player disconnects
        private void OnDisconnect(ulong client_id)
        {
            GameData gdata = GameData.Get();
            ClientData client = TheNetwork.Get().GetClient(client_id);
            if (client != null && client.player_id >= 0)
            {
                PlayerData user_player = gdata.GetPlayer(client.player_id);
                if (user_player != null)
                    user_player.connected = false;
            }
        }

        public static TheNetworkDemo Get()
        {
            if (instance == null)
                instance = FindObjectOfType<TheNetworkDemo>();
            return instance;
        }
    }

    [System.Serializable]
    public class DemoConnectData : INetworkSerializable
    {
        public GameMode mode = GameMode.None;
        public string character = "";

        public DemoConnectData() { }
        public DemoConnectData(GameMode m)
        {
            this.mode = m;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref mode);
            serializer.SerializeValue(ref character);
        }
    }
}
