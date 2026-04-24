using System;
using UnityEngine;
using Checkers.Data;
using Checkers.Network;
using Checkers.Utilities;

namespace Checkers.Core
{
    /// <summary>
    /// Central game orchestrator. Singleton MonoBehaviour that holds references to all
    /// major subsystems and coordinates game flow.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton

        private static GameManager _instance;

        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameLogger.Log(GameLogger.LogLevel.ERROR, "GameManager instance is null. Ensure it exists in the scene.");
                }
                return _instance;
            }
        }

        #endregion

        #region References

        [Header("Subsystem References")]
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private NetworkGameManager networkGameManager;

        [Header("Configuration")]
        [SerializeField] private BoardConfig boardConfig;
        [SerializeField] private PieceConfig pieceConfig;
        [SerializeField] private GameSettings gameSettings;

        public BoardManager BoardManager => boardManager;
        public TurnManager TurnManager => turnManager;
        public NetworkGameManager NetworkGameManager => networkGameManager;
        public BoardConfig BoardConfig => boardConfig;
        public PieceConfig PieceConfig => pieceConfig;
        public GameSettings GameSettings => gameSettings;

        #endregion

        #region Events

        /// <summary>Fired when the game starts after all players are ready.</summary>
        public event Action OnGameStart;

        /// <summary>Fired when the game ends. Parameter is the winner's actor number.</summary>
        public event Action<int> OnGameEnd;

        /// <summary>Fired when the active turn changes. Parameter is the current player's actor number.</summary>
        public event Action<int> OnTurnChanged;

        #endregion

        #region State

        private bool _gameActive;
        public bool IsGameActive => _gameActive;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                GameLogger.Log(GameLogger.LogLevel.WARN, "Duplicate GameManager detected. Destroying this instance.");
                Destroy(gameObject);
                return;
            }

            _instance = this;

            // Load configs from Resources if not assigned in Inspector
            if (boardConfig == null)
                boardConfig = Resources.Load<BoardConfig>("BoardConfig");
            if (pieceConfig == null)
                pieceConfig = Resources.Load<PieceConfig>("PieceConfig");
            if (gameSettings == null)
                gameSettings = Resources.Load<GameSettings>("GameSettings");

            if (boardConfig == null)
                GameLogger.Log(GameLogger.LogLevel.ERROR, "BoardConfig not found! Place it in Resources/BoardConfig.");
            if (pieceConfig == null)
                GameLogger.Log(GameLogger.LogLevel.ERROR, "PieceConfig not found! Place it in Resources/PieceConfig.");
            if (gameSettings == null)
                GameLogger.Log(GameLogger.LogLevel.ERROR, "GameSettings not found! Place it in Resources/GameSettings.");

            // Cache subsystem references if not assigned
            if (boardManager == null)
                boardManager = GetComponent<BoardManager>();
            if (turnManager == null)
                turnManager = GetComponent<TurnManager>();
            if (networkGameManager == null)
                networkGameManager = GetComponent<NetworkGameManager>();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        #endregion

        #region Game Flow

        /// <summary>
        /// Starts the game. Should be called by the MasterClient after all players have joined.
        /// Initializes the board and begins the first turn.
        /// </summary>
        public void StartGame()
        {
            if (boardConfig == null || gameSettings == null || pieceConfig == null)
            {
                GameLogger.Log(GameLogger.LogLevel.ERROR, "Cannot start game: configuration assets missing.");
                return;
            }

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Starting game — Board: {boardConfig.boardSize}x{boardConfig.boardSize}, Players: {boardConfig.playerCount}");

            boardManager.InitializeBoard(boardConfig.boardSize, boardConfig.playerCount);
            _gameActive = true;

            OnGameStart?.Invoke();

            // Trigger the first turn
            int firstPlayerActor = turnManager.GetCurrentPlayer();
            OnTurnChanged?.Invoke(firstPlayerActor);

            GameLogger.Log(GameLogger.LogLevel.INFO, $"Game started. First turn: Actor {firstPlayerActor}");
        }

        /// <summary>
        /// Ends the game with the specified winner.
        /// </summary>
        /// <param name="winnerActorNumber">Photon actor number of the winner. -1 for draw.</param>
        public void EndGame(int winnerActorNumber)
        {
            if (!_gameActive)
            {
                GameLogger.Log(GameLogger.LogLevel.WARN, "EndGame called but game is not active.");
                return;
            }

            _gameActive = false;
            GameLogger.Log(GameLogger.LogLevel.INFO, $"Game ended. Winner: Actor {winnerActorNumber}");

            OnGameEnd?.Invoke(winnerActorNumber);
        }

        /// <summary>
        /// Called when the turn advances to a new player.
        /// </summary>
        /// <param name="actorNumber">The actor number whose turn it now is.</param>
        public void NotifyTurnChanged(int actorNumber)
        {
            if (!_gameActive)
                return;

            OnTurnChanged?.Invoke(actorNumber);

            // Check win condition after turn change
            int[] activePlayers = turnManager.GetPlayerActorNumbers();
            int winner = WinConditionChecker.CheckWinCondition(
                boardManager.BoardState, activePlayers, gameSettings);

            if (winner != -1)
            {
                EndGame(winner);
            }
        }

        /// <summary>
        /// Restores the full game state from a serialized network payload.
        /// Used after reconnection.
        /// </summary>
        /// <param name="serializedState">JSON string of the board state.</param>
        public void RestoreStateFromNetwork(string serializedState)
        {
            if (string.IsNullOrEmpty(serializedState))
            {
                GameLogger.Log(GameLogger.LogLevel.WARN, "RestoreStateFromNetwork called with empty state.");
                return;
            }

            GameLogger.Log(GameLogger.LogLevel.INFO, "Restoring game state from network...");

            int restoredTurnIndex;
            PieceData[,] restoredBoard = StateSerializer.DeserializeBoard(serializedState, out restoredTurnIndex);

            if (restoredBoard == null)
            {
                GameLogger.Log(GameLogger.LogLevel.ERROR, "Failed to deserialize board state from network.");
                return;
            }

            boardManager.DeserializeBoardState(serializedState);
            turnManager.SetTurnIndex(restoredTurnIndex);
            _gameActive = true;

            int currentActor = turnManager.GetCurrentPlayer();
            OnTurnChanged?.Invoke(currentActor);

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"State restored. Turn index: {restoredTurnIndex}, Current actor: {currentActor}");
        }

        #endregion
    }
}
