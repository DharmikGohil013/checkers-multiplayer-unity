using System;
using UnityEngine;
using Checkers.Data;
using Checkers.Network;
using Checkers.Utilities;
namespace Checkers.Core
{
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
                    _instance = FindAnyObjectByType<GameManager>();
                    if (_instance == null)
                    {
                        GameLogger.Log(GameLogger.LogLevel.ERROR, "GameManager instance is null. Ensure it exists in the scene.");
                    }
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
        public event Action OnGameStart;
        public event Action<int> OnGameEnd;
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
                bool existingIsAlive = !ReferenceEquals(_instance, null) && ((UnityEngine.Object)_instance) != null;
                if (existingIsAlive)
                {
                    GameLogger.Log(GameLogger.LogLevel.WARN,
                        "Duplicate GameManager detected. Destroying this new instance.");
                    Destroy(gameObject);
                    return;
                }
                else
                {
                    GameLogger.Log(GameLogger.LogLevel.INFO,
                        "Previous GameManager was destroyed (scene reload). Replacing singleton reference.");
                }
            }
            _instance = this;
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
            if (boardManager == null)
                boardManager = GetComponent<BoardManager>();
            if (turnManager == null)
                turnManager = GetComponent<TurnManager>();
            if (networkGameManager == null)
                networkGameManager = GetComponent<NetworkGameManager>();
            Debug.Log($"[GameManager] Awake completed. Instance set. GameObject: {gameObject.name}");
        }
        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
            Debug.Log("[GameManager] OnDestroy called. Singleton reference cleared.");
        }
        #endregion
        #region Game Flow
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
            int firstPlayerActor = turnManager.GetCurrentPlayer();
            OnTurnChanged?.Invoke(firstPlayerActor);
            GameLogger.Log(GameLogger.LogLevel.INFO, $"Game started. First turn: Actor {firstPlayerActor}");
        }
        public void EndGame(int winnerActorNumber)
        {
            if (!_gameActive)
            {
                GameLogger.Log(GameLogger.LogLevel.WARN, "EndGame called but game is already ended. Firing event anyway.");
                OnGameEnd?.Invoke(winnerActorNumber);
                return;
            }
            _gameActive = false;
            GameLogger.Log(GameLogger.LogLevel.INFO, $"Game ended. Winner: Actor {winnerActorNumber}");
            OnGameEnd?.Invoke(winnerActorNumber);
        }
        public void NotifyTurnChanged(int actorNumber)
        {
            if (!_gameActive)
                return;
            OnTurnChanged?.Invoke(actorNumber);
            if (!Photon.Pun.PhotonNetwork.IsMasterClient)
                return;
            int[] actorNumbers = turnManager.GetPlayerActorNumbers();
            if (actorNumbers == null || actorNumbers.Length < 2)
                return;
            int[] playerOwners = new int[actorNumbers.Length];
            for (int i = 0; i < actorNumbers.Length; i++)
                playerOwners[i] = i + 1;
            int winnerOwnerIndex = WinConditionChecker.CheckWinCondition(
                boardManager.BoardState, playerOwners, gameSettings);
            if (winnerOwnerIndex != -1)
            {
                int winnerActorNumber = actorNumbers[winnerOwnerIndex - 1];
                networkGameManager.SendGameOver(winnerActorNumber);
            }
        }
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
