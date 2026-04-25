using System;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Checkers.Data;
using Checkers.Utilities;

namespace Checkers.Core
{
    /// <summary>
    /// Manages turn order, timer countdown, and turn transitions.
    /// Synchronized via Photon Room Properties.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        #region Fields

        [Header("Configuration")]
        [SerializeField] private GameSettings gameSettings;

        /// <summary>Array of Photon Actor Numbers in play order.</summary>
        private int[] _playerActorNumbers;

        /// <summary>Current turn index into _playerActorNumbers.</summary>
        private int _currentTurnIndex;

        /// <summary>Remaining time for the current turn.</summary>
        private float _turnTimer;

        /// <summary>Whether the timer is actively counting down.</summary>
        private bool _timerActive;

        public int CurrentTurnIndex => _currentTurnIndex;

        #endregion

        #region Events

        /// <summary>Fired when a new turn starts. Parameter is the actor number of the active player.</summary>
        public event Action<int> OnTurnStart;

        /// <summary>Fired when the current turn times out. Parameter is the actor number whose turn expired.</summary>
        public event Action<int> OnTurnTimeout;

        /// <summary>Fired every frame while the timer is active. Parameter is remaining time.</summary>
        public event Action<float> OnTimerUpdate;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Do NOT access GameManager.Instance here — Awake execution order
            // is not guaranteed, and GameManager.Awake may not have run yet.
            // gameSettings will be loaded lazily in EnsureSettings().
        }

        private void EnsureSettings()
        {
            if (gameSettings == null)
            {
                if (GameManager.Instance != null)
                    gameSettings = GameManager.Instance.GameSettings;

                if (gameSettings == null)
                    gameSettings = Resources.Load<GameSettings>("GameSettings");
            }
        }

        private void Update()
        {
            EnsureSettings();

            if (!_timerActive || gameSettings == null)
                return;

            if (gameSettings.turnTimeLimit <= 0f)
                return;

            _turnTimer -= Time.deltaTime;
            OnTimerUpdate?.Invoke(_turnTimer);

            if (_turnTimer <= 0f)
            {
                _turnTimer = 0f;
                _timerActive = false;

                int timedOutActor = GetCurrentPlayer();
                GameLogger.Log(GameLogger.LogLevel.WARN,
                    $"Turn timeout for Actor {timedOutActor}");

                OnTurnTimeout?.Invoke(timedOutActor);

                // Auto-advance turn on timeout (only MasterClient advances, via network RPC)
                if (PhotonNetwork.IsMasterClient)
                {
                    if (GameManager.Instance != null && GameManager.Instance.NetworkGameManager != null)
                    {
                        GameManager.Instance.NetworkGameManager.SendEndTurn();
                    }
                    else
                    {
                        NextTurn(); // Fallback for local testing
                    }
                }
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the player order from Photon actor numbers.
        /// Should be called when the game starts.
        /// </summary>
        public void InitializePlayers(int[] actorNumbers)
        {
            if (actorNumbers == null || actorNumbers.Length == 0)
            {
                GameLogger.Log(GameLogger.LogLevel.ERROR, "InitializePlayers called with empty array.");
                return;
            }

            EnsureSettings();

            _playerActorNumbers = new int[actorNumbers.Length];
            Array.Copy(actorNumbers, _playerActorNumbers, actorNumbers.Length);

            _currentTurnIndex = 0;
            ResetTimer();

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Players initialized. Count: {actorNumbers.Length}. First turn: Actor {_playerActorNumbers[0]}");

            // Sync initial turn index to room properties
            SyncTurnToRoomProperties();

            OnTurnStart?.Invoke(_playerActorNumbers[_currentTurnIndex]);
        }

        #endregion

        #region Turn Operations

        /// <summary>
        /// Advances to the next player's turn.
        /// Wraps around if at the end of the player array.
        /// </summary>
        public void NextTurn()
        {
            if (_playerActorNumbers == null || _playerActorNumbers.Length == 0)
            {
                GameLogger.Log(GameLogger.LogLevel.ERROR, "NextTurn called but no players initialized.");
                return;
            }

            _currentTurnIndex = (_currentTurnIndex + 1) % _playerActorNumbers.Length;
            ResetTimer();

            int nextActor = _playerActorNumbers[_currentTurnIndex];

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Turn advanced. Index: {_currentTurnIndex}, Actor: {nextActor}");

            // Sync to room properties
            SyncTurnToRoomProperties();

            OnTurnStart?.Invoke(nextActor);

            // Notify GameManager
            if (GameManager.Instance != null)
                GameManager.Instance.NotifyTurnChanged(nextActor);
        }

        /// <summary>
        /// Returns the Photon Actor Number of the current player.
        /// </summary>
        public int GetCurrentPlayer()
        {
            if (_playerActorNumbers == null || _playerActorNumbers.Length == 0)
                return -1;

            return _playerActorNumbers[_currentTurnIndex];
        }

        /// <summary>
        /// Returns the full array of player actor numbers in play order.
        /// </summary>
        public int[] GetPlayerActorNumbers()
        {
            return _playerActorNumbers;
        }

        /// <summary>
        /// Resets the turn timer to the configured limit.
        /// </summary>
        public void ResetTimer()
        {
            if (gameSettings != null && gameSettings.turnTimeLimit > 0f)
            {
                _turnTimer = gameSettings.turnTimeLimit;
                _timerActive = true;
            }
            else
            {
                _timerActive = false;
            }
        }

        /// <summary>
        /// Checks whether it is the local Photon client's turn.
        /// </summary>
        public bool IsLocalPlayerTurn()
        {
            if (!PhotonNetwork.IsConnected || PhotonNetwork.LocalPlayer == null)
                return false;

            int currentTurnPlayer = GetCurrentPlayer();
            int myActor = PhotonNetwork.LocalPlayer.ActorNumber;

            // Optional: uncomment these if you need to spam the log every frame during input check
            // Debug.Log("[TurnManager] Current Turn Player: " + currentTurnPlayer);
            // Debug.Log("[TurnManager] My Actor: " + myActor);

            return currentTurnPlayer == myActor;
        }

        /// <summary>
        /// Sets the turn index directly (used for state restoration on reconnect).
        /// </summary>
        public void SetTurnIndex(int index)
        {
            if (_playerActorNumbers == null || _playerActorNumbers.Length == 0)
            {
                GameLogger.Log(GameLogger.LogLevel.WARN, "SetTurnIndex called but players not initialized.");
                return;
            }

            _currentTurnIndex = Mathf.Clamp(index, 0, _playerActorNumbers.Length - 1);
            ResetTimer();

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Turn index set to {_currentTurnIndex}, Actor: {GetCurrentPlayer()}");
        }

        /// <summary>
        /// Removes a player from the turn order (e.g., on disconnect or elimination).
        /// </summary>
        public void RemovePlayer(int actorNumber)
        {
            if (_playerActorNumbers == null)
                return;

            int index = Array.IndexOf(_playerActorNumbers, actorNumber);
            if (index == -1)
                return;

            int[] newArray = new int[_playerActorNumbers.Length - 1];
            int j = 0;
            for (int i = 0; i < _playerActorNumbers.Length; i++)
            {
                if (i != index)
                    newArray[j++] = _playerActorNumbers[i];
            }

            _playerActorNumbers = newArray;

            if (_currentTurnIndex >= _playerActorNumbers.Length)
                _currentTurnIndex = 0;

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Player Actor {actorNumber} removed from turn order. Remaining: {_playerActorNumbers.Length}");
        }

        /// <summary>
        /// Returns the remaining time on the current turn timer.
        /// </summary>
        public float GetRemainingTime()
        {
            return _timerActive ? _turnTimer : 0f;
        }

        #endregion

        #region Network Sync

        private void SyncTurnToRoomProperties()
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
                return;

            Hashtable props = new Hashtable
            {
                { Constants.ROOM_PROP_TURN_INDEX, _currentTurnIndex }
            };

            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        /// <summary>
        /// Restores turn index from room properties (used on reconnect).
        /// </summary>
        public void RestoreFromRoomProperties()
        {
            if (!PhotonNetwork.InRoom)
                return;

            object turnIndexObj;
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(Constants.ROOM_PROP_TURN_INDEX, out turnIndexObj))
            {
                int restoredIndex = (int)turnIndexObj;
                SetTurnIndex(restoredIndex);
                GameLogger.Log(GameLogger.LogLevel.INFO, $"Turn index restored from room properties: {restoredIndex}");
            }
        }

        #endregion
    }
}
