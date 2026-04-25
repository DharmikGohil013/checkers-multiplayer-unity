using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Checkers.Core;
using Checkers.Utilities;

namespace Checkers.Network
{
    /// <summary>
    /// Handles in-game networking: move RPCs, state synchronization, and room property updates.
    /// Requires a PhotonView component on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class NetworkGameManager : MonoBehaviourPunCallbacks
    {
        #region Fields

        private PhotonView _photonView;
        private GameManager _gameManager;
        private BoardManager _boardManager;
        private TurnManager _turnManager;
        private bool _gameInitialized = false;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _photonView = GetComponent<PhotonView>();
        }

        private void Start()
        {
            CacheReferences();

            // If MasterClient, initialize the game
            if (PhotonNetwork.IsMasterClient)
            {
                InitializeGame();
            }
        }

        /// <summary>
        /// Re-cache references in case they were lost (e.g., after scene reload).
        /// </summary>
        private void CacheReferences()
        {
            _gameManager = GameManager.Instance;
            if (_gameManager != null)
            {
                _boardManager = _gameManager.BoardManager;
                _turnManager = _gameManager.TurnManager;
            }
            else
            {
                Debug.LogError("[NetworkGameManager] GameManager.Instance is null! Ensure GameManagers object exists in GameScene.");
            }
        }

        /// <summary>
        /// Ensures references are valid. Call before any operation that needs them.
        /// </summary>
        private void EnsureReferences()
        {
            if (_gameManager == null || _boardManager == null || _turnManager == null)
            {
                CacheReferences();
            }
        }

        #endregion

        #region Game Initialization

        /// <summary>
        /// Called by MasterClient to set up the game for all players.
        /// Distributes player actor numbers and starts the game.
        /// </summary>
        private void InitializeGame()
        {
            if (!PhotonNetwork.IsMasterClient)
                return;

            Player[] players = PhotonNetwork.PlayerList;
            int[] actorNumbers = new int[players.Length];

            for (int i = 0; i < players.Length; i++)
            {
                actorNumbers[i] = players[i].ActorNumber;
            }

            // Sort by actor number for deterministic order
            System.Array.Sort(actorNumbers);

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Initializing game with {actorNumbers.Length} players: [{string.Join(", ", actorNumbers)}]");

            // Send initialization RPC to all players
            _photonView.RPC(nameof(RPC_InitializeGame), RpcTarget.All, actorNumbers);
        }

        [PunRPC]
        private void RPC_InitializeGame(int[] actorNumbers)
        {
            EnsureReferences();

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"RPC_InitializeGame received. Players: [{string.Join(", ", actorNumbers)}]");

            if (_turnManager != null)
                _turnManager.InitializePlayers(actorNumbers);

            if (_gameManager != null)
                _gameManager.StartGame();

            _gameInitialized = true;
        }

        #endregion

        #region Move Execution

        /// <summary>
        /// Sends a move to all players via RPC.
        /// Called by the local player's InputHandler.
        /// </summary>
        public void SendMove(MoveData move)
        {
            EnsureReferences();

            if (_turnManager == null || !_turnManager.IsLocalPlayerTurn())
            {
                GameLogger.Log(GameLogger.LogLevel.WARN, "SendMove called but it's not local player's turn.");
                return;
            }

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Sending move: ({move.fromRow},{move.fromCol}) → ({move.toRow},{move.toCol}), " +
                $"Capture: {move.isCapture} at ({move.captureRow},{move.captureCol})");

            _photonView.RPC(nameof(RPC_ExecuteMove), RpcTarget.All,
                move.fromRow, move.fromCol, move.toRow, move.toCol,
                move.captureRow, move.captureCol);
        }

        /// <summary>
        /// RPC executed on all clients to apply a move.
        /// </summary>
        [PunRPC]
        private void RPC_ExecuteMove(int fromRow, int fromCol, int toRow, int toCol,
            int captureRow, int captureCol, PhotonMessageInfo info)
        {
            EnsureReferences();

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"RPC_ExecuteMove from Actor {info.Sender.ActorNumber}: " +
                $"({fromRow},{fromCol}) → ({toRow},{toCol}), Capture: ({captureRow},{captureCol})");

            if (_boardManager == null)
            {
                GameLogger.Log(GameLogger.LogLevel.ERROR, "BoardManager is null in RPC_ExecuteMove.");
                return;
            }

            // Execute capture first if applicable
            if (captureRow >= 0 && captureCol >= 0)
            {
                _boardManager.CapturePiece(captureRow, captureCol);
            }

            // Execute move
            _boardManager.MovePiece(fromRow, fromCol, toRow, toCol);

            // Check for king promotion
            PieceData movedPiece = _boardManager.BoardState[toRow, toCol];
            if (!movedPiece.isKing && _gameManager != null && _gameManager.GameSettings != null && _gameManager.GameSettings.kingPromotion)
            {
                if (CheckersRules.CanPromoteToKing(toRow, movedPiece.playerOwner, _boardManager.BoardSize))
                {
                    _boardManager.PromoteToKing(toRow, toCol);
                }
            }

            // Check for multi-jump
            if (captureRow >= 0 && captureCol >= 0)
            {
                if (_gameManager != null && CheckersRules.IsMultiJumpAvailable(_boardManager.BoardState, toRow, toCol, _gameManager.GameSettings))
                {
                    GameLogger.Log(GameLogger.LogLevel.INFO,
                        $"Multi-jump available at ({toRow},{toCol}). Turn continues.");
                    // Don't end turn — player continues with multi-jump
                    return;
                }
            }

            // End turn — advance to next player
            if (PhotonNetwork.IsMasterClient)
            {
                // Sync board state to room properties for reconnect support
                SyncFullState();

                if (_turnManager != null)
                {
                    _photonView.RPC(nameof(RPC_EndTurn), RpcTarget.All,
                        _turnManager.GetCurrentPlayer());
                }
            }
        }

        /// <summary>
        /// Sends an RPC to end the turn. Called on timeout.
        /// </summary>
        public void SendEndTurn()
        {
            EnsureReferences();
            if (_turnManager != null)
            {
                _photonView.RPC(nameof(RPC_EndTurn), RpcTarget.All, _turnManager.GetCurrentPlayer());
            }
        }

        /// <summary>
        /// RPC to end the current turn and advance to the next player.
        /// </summary>
        [PunRPC]
        private void RPC_EndTurn(int previousActorNumber, PhotonMessageInfo info)
        {
            EnsureReferences();

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"RPC_EndTurn received. Previous player: Actor {previousActorNumber}");

            if (_turnManager != null)
                _turnManager.NextTurn();
        }

        /// <summary>
        /// RPC to end the game with a declared winner.
        /// </summary>
        [PunRPC]
        private void RPC_GameOver(int winnerActorNumber, PhotonMessageInfo info)
        {
            EnsureReferences();

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"RPC_GameOver received. Winner: Actor {winnerActorNumber}");

            if (_gameManager != null)
                _gameManager.EndGame(winnerActorNumber);
        }

        /// <summary>
        /// Sends game over RPC to all clients.
        /// Can be called by any player (e.g., resign).
        /// </summary>
        public void SendGameOver(int winnerActorNumber)
        {
            _photonView.RPC(nameof(RPC_GameOver), RpcTarget.All, winnerActorNumber);
        }

        /// <summary>
        /// RPC to synchronize the full board state (used on reconnect or desync fix).
        /// </summary>
        [PunRPC]
        private void RPC_SyncBoardState(string serializedState, PhotonMessageInfo info)
        {
            EnsureReferences();

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"RPC_SyncBoardState received from Actor {info.Sender.ActorNumber}. " +
                $"State length: {serializedState.Length}");

            if (_gameManager != null)
                _gameManager.RestoreStateFromNetwork(serializedState);
        }

        #endregion

        #region State Synchronization

        /// <summary>
        /// Serializes the current board state and stores it in Photon Room Properties.
        /// Also sends an RPC for immediate sync. Called by MasterClient after each move.
        /// </summary>
        public void SyncFullState()
        {
            EnsureReferences();

            if (_boardManager == null)
                return;

            string serialized = _boardManager.SerializeBoardState();

            // Store in room properties for reconnect support
            if (PhotonNetwork.InRoom)
            {
                Hashtable props = new Hashtable
                {
                    { Constants.ROOM_PROP_GAME_STATE, serialized }
                };
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            }

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Full state synced to room properties. Length: {serialized.Length}");
        }

        /// <summary>
        /// Sends full board state sync to a specific player (e.g., on reconnect).
        /// </summary>
        public void SendFullStateToPlayer(Player targetPlayer)
        {
            EnsureReferences();

            if (_boardManager == null)
                return;

            string serialized = _boardManager.SerializeBoardState();
            _photonView.RPC(nameof(RPC_SyncBoardState), targetPlayer, serialized);

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Sent full state to Actor {targetPlayer.ActorNumber}");
        }

        #endregion

        #region Photon Callbacks

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            EnsureReferences();

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Player left during game: {otherPlayer.NickName} (Actor {otherPlayer.ActorNumber})");

            if (!PhotonNetwork.IsMasterClient)
                return;

            if (!_gameInitialized)
                return;

            // Remove player from turn order
            if (_turnManager != null)
                _turnManager.RemovePlayer(otherPlayer.ActorNumber);

            // Check if enough players remain to continue
            if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
            {
                // Only one player left — they win
                Player[] remaining = PhotonNetwork.PlayerList;
                if (remaining.Length > 0)
                {
                    int winnerId = remaining[0].ActorNumber;
                    GameLogger.Log(GameLogger.LogLevel.INFO,
                        $"Not enough players. Declaring Actor {winnerId} as winner.");
                    SendGameOver(winnerId);
                }
            }
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            EnsureReferences();

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Player re-entered during game: {newPlayer.NickName} (Actor {newPlayer.ActorNumber})");

            // If MasterClient, send current state to reconnected player
            if (PhotonNetwork.IsMasterClient && _gameInitialized)
            {
                SendFullStateToPlayer(newPlayer);
            }
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            EnsureReferences();

            GameLogger.Log(GameLogger.LogLevel.WARN,
                $"MasterClient switched to: {newMasterClient.NickName} (Actor {newMasterClient.ActorNumber})");

            // New MasterClient should sync state
            if (PhotonNetwork.IsMasterClient)
            {
                SyncFullState();
            }
        }

        #endregion
    }
}
