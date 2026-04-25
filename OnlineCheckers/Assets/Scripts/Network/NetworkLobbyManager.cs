using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Checkers.Data;
using Checkers.UI;
using Checkers.Utilities;
namespace Checkers.Network
{
    public class NetworkLobbyManager : MonoBehaviourPunCallbacks
    {
        [Header("Configuration")]
        [SerializeField] private BoardConfig boardConfig;
        [SerializeField] private GameSettings gameSettings;
        [Header("UI Reference")]
        [SerializeField] private LobbyUIController lobbyUI;
        private bool _joinRandomOnConnect;
        #region Unity Lifecycle
        private void Awake()
        {
            PhotonNetwork.AutomaticallySyncScene = true;
        }
        private void Start()
        {
            if (lobbyUI == null)
                lobbyUI = FindAnyObjectByType<LobbyUIController>();
            if (!PhotonNetwork.IsConnected)
                ConnectToPhoton();
            else
                lobbyUI?.SetStatusText("Connected to Photon.");
        }
        #endregion
        #region Connection
        public void ConnectToPhoton()
        {
            if (PhotonNetwork.IsConnected) return;
            lobbyUI?.SetStatusText("Connecting to Photon...");
            PhotonNetwork.GameVersion = gameSettings != null ? gameSettings.photonAppVersion : "1.0";
            PhotonNetwork.ConnectUsingSettings();
        }
        #endregion
        #region Room Operations
        public void CreateRoom(string roomName)
        {
            if (!PhotonNetwork.IsConnectedAndReady)
            {
                lobbyUI?.SetStatusText("Not connected. Please wait...");
                return;
            }
            if (string.IsNullOrEmpty(roomName))
                roomName = "Room_" + Random.Range(1000, 9999);
            int maxPlayers = boardConfig != null ? boardConfig.playerCount : 2;
            int boardSize  = boardConfig != null ? boardConfig.boardSize  : 8;
            RoomOptions options = new RoomOptions
            {
                MaxPlayers = (byte)maxPlayers,
                IsVisible  = true,
                IsOpen     = true,
                PlayerTtl  = 60000,   
                EmptyRoomTtl = 0,
                CustomRoomProperties = new Hashtable
                {
                    { Constants.ROOM_PROP_BOARD_SIZE,   boardSize   },
                    { Constants.ROOM_PROP_PLAYER_COUNT, maxPlayers  },
                    { Constants.ROOM_PROP_GAME_STATE,   ""          }
                },
                CustomRoomPropertiesForLobby = new string[]
                {
                    Constants.ROOM_PROP_BOARD_SIZE,
                    Constants.ROOM_PROP_PLAYER_COUNT
                }
            };
            PhotonNetwork.CreateRoom(roomName, options);
            lobbyUI?.SetStatusText($"Creating room '{roomName}'...");
            Debug.Log($"[Lobby] Creating room: {roomName}");
        }
        public void JoinRoom(string roomName)
        {
            if (!PhotonNetwork.IsConnectedAndReady)
            {
                lobbyUI?.SetStatusText("Not connected. Please wait...");
                return;
            }
            if (string.IsNullOrEmpty(roomName))
            {
                lobbyUI?.SetStatusText("Please enter a room name.");
                return;
            }
            PhotonNetwork.JoinRoom(roomName);
            lobbyUI?.SetStatusText($"Joining room '{roomName}'...");
            Debug.Log($"[Lobby] Joining room: {roomName}");
        }
        public void JoinRandomRoom()
        {
            if (!PhotonNetwork.IsConnectedAndReady)
            {
                _joinRandomOnConnect = true;
                ConnectToPhoton();
                return;
            }
            PhotonNetwork.JoinRandomRoom();
            lobbyUI?.SetStatusText("Searching for a room...");
        }
        public void StartGame()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
            {
                lobbyUI?.SetStatusText("Need at least 2 players.");
                return;
            }
            PhotonNetwork.CurrentRoom.IsOpen = false;
            Debug.Log("[Lobby] Starting game...");
            PhotonNetwork.LoadLevel(Constants.SCENE_GAME);
        }
        public void SetBoardSize(int size)
        {
            if (boardConfig != null)
                boardConfig.boardSize = size;
        }
        public void SetPlayerCount(int count)
        {
            if (boardConfig != null)
                boardConfig.playerCount = count;
        }
        public void Disconnect()
        {
            if (PhotonNetwork.IsConnected)
                PhotonNetwork.Disconnect();
        }
        #endregion
        #region Photon Callbacks
        public override void OnConnectedToMaster()
        {
            Debug.Log("[Lobby] Connected to Master.");
            lobbyUI?.SetStatusText("Connected! Create or join a room.");
            if (_joinRandomOnConnect)
            {
                _joinRandomOnConnect = false;
                JoinRandomRoom();
            }
            else
            {
                PhotonNetwork.JoinLobby();
            }
        }
        public override void OnJoinedLobby()
        {
            Debug.Log("[Lobby] Joined Photon Lobby.");
            lobbyUI?.SetStatusText("In lobby. Create or join a room.");
        }
        public override void OnJoinedRoom()
        {
            Debug.Log($"[Lobby] Joined room: {PhotonNetwork.CurrentRoom.Name} " +
                      $"({PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers})");
            lobbyUI?.SetStatusText($"In room '{PhotonNetwork.CurrentRoom.Name}'. " +
                                   $"Players: {PhotonNetwork.CurrentRoom.PlayerCount}");
            lobbyUI?.OnJoinedRoom();
            lobbyUI?.UpdatePlayerList(PhotonNetwork.PlayerList);
        }
        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log($"[Lobby] Player joined: {newPlayer.NickName} " +
                      $"(Actor {newPlayer.ActorNumber}). " +
                      $"Total: {PhotonNetwork.CurrentRoom.PlayerCount}");
            lobbyUI?.SetStatusText($"{newPlayer.NickName} joined! " +
                                   $"Players: {PhotonNetwork.CurrentRoom.PlayerCount}");
            lobbyUI?.UpdatePlayerList(PhotonNetwork.PlayerList);
        }
        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"[Lobby] Player left: {otherPlayer.NickName}");
            lobbyUI?.SetStatusText($"{otherPlayer.NickName} left.");
            lobbyUI?.UpdatePlayerList(PhotonNetwork.PlayerList);
        }
        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log($"[Lobby] Join random failed: {message}. Creating room.");
            lobbyUI?.SetStatusText("No rooms found. Creating one...");
            CreateRoom("Room_" + Random.Range(1000, 9999));
        }
        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogWarning($"[Lobby] Join room failed: {message}");
            lobbyUI?.SetStatusText($"Failed to join: {message}");
        }
        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[Lobby] Create room failed: {message}");
            lobbyUI?.SetStatusText($"Failed to create room: {message}");
        }
        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogWarning($"[Lobby] Disconnected: {cause}");
            lobbyUI?.SetStatusText($"Disconnected: {cause}");
        }
        #endregion
    }
}