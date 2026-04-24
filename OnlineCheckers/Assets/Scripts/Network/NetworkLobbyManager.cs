using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Checkers.Data;
using Checkers.UI;
using Checkers.Utilities;

namespace Checkers.Network
{
    /// <summary>
    /// Handles Photon lobby operations: connecting, creating/joining rooms, and scene transitions.
    /// Attached to a persistent lobby GameObject in the LobbyScene.
    /// </summary>
    public class NetworkLobbyManager : MonoBehaviourPunCallbacks
    {
        #region Fields

        [Header("Configuration")]
        [SerializeField] private BoardConfig boardConfig;
        [SerializeField] private GameSettings gameSettings;

        [Header("UI Reference")]
        [SerializeField] private LobbyUIController lobbyUI;

        private bool _isConnecting;
        private string _pendingRoomName;
        private bool _joinRandomOnConnect;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Load configs from Resources if not assigned
            if (boardConfig == null)
                boardConfig = Resources.Load<BoardConfig>("BoardConfig");
            if (gameSettings == null)
                gameSettings = Resources.Load<GameSettings>("GameSettings");

            // Ensure we don't destroy this across scenes (lobby manager persists until game scene loads)
            PhotonNetwork.AutomaticallySyncScene = true;
        }

        private void Start()
        {
            if (lobbyUI == null)
                lobbyUI = FindAnyObjectByType<LobbyUIController>();

            if (!PhotonNetwork.IsConnected)
            {
                ConnectToPhoton();
            }
            else
            {
                UpdateUIStatus("Connected to Photon.");
            }
        }

        #endregion

        #region Connection

        /// <summary>
        /// Connects to Photon master server using GameSettings configuration.
        /// </summary>
        public void ConnectToPhoton()
        {
            if (PhotonNetwork.IsConnected)
            {
                GameLogger.Log(GameLogger.LogLevel.INFO, "Already connected to Photon.");
                return;
            }

            _isConnecting = true;
            UpdateUIStatus("Connecting to Photon...");

            PhotonNetwork.GameVersion = gameSettings != null ? gameSettings.photonAppVersion : Constants.PHOTON_APP_VERSION;
            PhotonNetwork.ConnectUsingSettings();

            GameLogger.Log(GameLogger.LogLevel.INFO, "Connecting to Photon...");
        }

        #endregion

        #region Room Operations

        /// <summary>
        /// Creates a new room with the specified name and current board configuration.
        /// </summary>
        public void CreateRoom(string roomName)
        {
            if (!PhotonNetwork.IsConnectedAndReady)
            {
                GameLogger.Log(GameLogger.LogLevel.WARN, "Cannot create room: not connected.");
                UpdateUIStatus("Not connected. Please wait...");
                return;
            }

            if (string.IsNullOrEmpty(roomName))
                roomName = "Room_" + Random.Range(1000, 9999);

            int maxPlayers = boardConfig != null ? boardConfig.playerCount : 2;
            int boardSize = boardConfig != null ? boardConfig.boardSize : 8;

            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = (byte)maxPlayers,
                IsVisible = true,
                IsOpen = true,
                PlayerTtl = 30000, // 30 seconds reconnect window
                EmptyRoomTtl = 0,
                CustomRoomProperties = new Hashtable
                {
                    { Constants.ROOM_PROP_BOARD_SIZE, boardSize },
                    { Constants.ROOM_PROP_PLAYER_COUNT, maxPlayers },
                    { Constants.ROOM_PROP_GAME_STATE, "" }
                },
                CustomRoomPropertiesForLobby = new string[]
                {
                    Constants.ROOM_PROP_BOARD_SIZE,
                    Constants.ROOM_PROP_PLAYER_COUNT
                }
            };

            PhotonNetwork.CreateRoom(roomName, roomOptions);
            UpdateUIStatus($"Creating room '{roomName}'...");

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Creating room: {roomName}, MaxPlayers: {maxPlayers}, BoardSize: {boardSize}");
        }

        /// <summary>
        /// Joins an existing room by name.
        /// </summary>
        public void JoinRoom(string roomName)
        {
            if (!PhotonNetwork.IsConnectedAndReady)
            {
                UpdateUIStatus("Not connected. Please wait...");
                return;
            }

            if (string.IsNullOrEmpty(roomName))
            {
                UpdateUIStatus("Please enter a room name.");
                return;
            }

            PhotonNetwork.JoinRoom(roomName);
            UpdateUIStatus($"Joining room '{roomName}'...");

            GameLogger.Log(GameLogger.LogLevel.INFO, $"Joining room: {roomName}");
        }

        /// <summary>
        /// Attempts to join a random available room.
        /// </summary>
        public void JoinRandomRoom()
        {
            if (!PhotonNetwork.IsConnectedAndReady)
            {
                _joinRandomOnConnect = true;
                UpdateUIStatus("Connecting first...");
                ConnectToPhoton();
                return;
            }

            int boardSize = boardConfig != null ? boardConfig.boardSize : 8;
            int playerCount = boardConfig != null ? boardConfig.playerCount : 2;

            Hashtable expectedProps = new Hashtable
            {
                { Constants.ROOM_PROP_BOARD_SIZE, boardSize },
                { Constants.ROOM_PROP_PLAYER_COUNT, playerCount }
            };

            PhotonNetwork.JoinRandomRoom(expectedProps, 0);
            UpdateUIStatus("Searching for a room...");

            GameLogger.Log(GameLogger.LogLevel.INFO, "Joining random room...");
        }

        /// <summary>
        /// Starts the game (MasterClient only). Loads the game scene for all players.
        /// </summary>
        public void StartGame()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                GameLogger.Log(GameLogger.LogLevel.WARN, "Only MasterClient can start the game.");
                return;
            }

            if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
            {
                UpdateUIStatus("Need at least 2 players to start.");
                return;
            }

            // Close the room so no new players can join
            PhotonNetwork.CurrentRoom.IsOpen = false;

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Starting game with {PhotonNetwork.CurrentRoom.PlayerCount} players.");

            PhotonNetwork.LoadLevel(Constants.SCENE_GAME);
        }

        /// <summary>
        /// Updates the board configuration based on UI selections.
        /// </summary>
        public void SetBoardSize(int size)
        {
            if (boardConfig != null)
                boardConfig.boardSize = size;
        }

        /// <summary>
        /// Updates the player count configuration based on UI selections.
        /// </summary>
        public void SetPlayerCount(int count)
        {
            if (boardConfig != null)
                boardConfig.playerCount = count;
        }

        /// <summary>
        /// Disconnects from Photon and returns to the main menu.
        /// </summary>
        public void Disconnect()
        {
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
                GameLogger.Log(GameLogger.LogLevel.INFO, "Disconnected from Photon.");
            }
        }

        #endregion

        #region Photon Callbacks

        public override void OnConnectedToMaster()
        {
            _isConnecting = false;
            UpdateUIStatus("Connected to Photon. Ready to play!");

            GameLogger.Log(GameLogger.LogLevel.INFO, "Connected to Photon Master Server.");

            if (_joinRandomOnConnect)
            {
                _joinRandomOnConnect = false;
                JoinRandomRoom();
            }

            PhotonNetwork.JoinLobby();
        }

        public override void OnJoinedLobby()
        {
            GameLogger.Log(GameLogger.LogLevel.INFO, "Joined Photon Lobby.");
            UpdateUIStatus("In lobby. Create or join a room.");
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Join random failed ({returnCode}): {message}. Creating new room...");

            UpdateUIStatus("No rooms found. Creating one...");
            CreateRoom("Room_" + Random.Range(1000, 9999));
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            GameLogger.Log(GameLogger.LogLevel.WARN, $"Join room failed ({returnCode}): {message}");
            UpdateUIStatus($"Failed to join room: {message}");
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            GameLogger.Log(GameLogger.LogLevel.ERROR, $"Create room failed ({returnCode}): {message}");
            UpdateUIStatus($"Failed to create room: {message}");
        }

        public override void OnJoinedRoom()
        {
            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Joined room: {PhotonNetwork.CurrentRoom.Name}. Players: {PhotonNetwork.CurrentRoom.PlayerCount}");

            UpdateUIStatus($"Joined room '{PhotonNetwork.CurrentRoom.Name}'. Waiting for players...");

            if (lobbyUI != null)
            {
                lobbyUI.OnJoinedRoom();
                lobbyUI.UpdatePlayerList(PhotonNetwork.PlayerList);
                lobbyUI.SetStartButtonVisible(PhotonNetwork.IsMasterClient &&
                    PhotonNetwork.CurrentRoom.PlayerCount >= 2);
            }
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Player joined: {newPlayer.NickName} (Actor {newPlayer.ActorNumber}). " +
                $"Total: {PhotonNetwork.CurrentRoom.PlayerCount}");

            UpdateUIStatus($"{newPlayer.NickName} joined! Players: {PhotonNetwork.CurrentRoom.PlayerCount}");

            if (lobbyUI != null)
            {
                lobbyUI.UpdatePlayerList(PhotonNetwork.PlayerList);
                lobbyUI.SetStartButtonVisible(PhotonNetwork.IsMasterClient &&
                    PhotonNetwork.CurrentRoom.PlayerCount >= 2);
            }
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Player left: {otherPlayer.NickName} (Actor {otherPlayer.ActorNumber}). " +
                $"Total: {PhotonNetwork.CurrentRoom.PlayerCount}");

            UpdateUIStatus($"{otherPlayer.NickName} left. Players: {PhotonNetwork.CurrentRoom.PlayerCount}");

            if (lobbyUI != null)
            {
                lobbyUI.UpdatePlayerList(PhotonNetwork.PlayerList);
                lobbyUI.SetStartButtonVisible(PhotonNetwork.IsMasterClient &&
                    PhotonNetwork.CurrentRoom.PlayerCount >= 2);
            }
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            _isConnecting = false;
            GameLogger.Log(GameLogger.LogLevel.WARN, $"Disconnected from Photon: {cause}");
            UpdateUIStatus($"Disconnected: {cause}. Press Connect to try again.");
        }

        #endregion

        #region Helpers

        private void UpdateUIStatus(string message)
        {
            if (lobbyUI != null)
                lobbyUI.SetStatusText(message);
        }

        #endregion
    }
}
