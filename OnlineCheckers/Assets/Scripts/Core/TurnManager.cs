using System;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Checkers.Data;
using Checkers.Utilities;
namespace Checkers.Core
{
    public class TurnManager : MonoBehaviour
    {
        #region Fields
        [Header("Configuration")]
        [SerializeField] private GameSettings gameSettings;
        private int[] _playerActorNumbers;
        private int _currentTurnIndex;
        private float _turnTimer;
        private bool _timerActive;
        public int CurrentTurnIndex => _currentTurnIndex;
        #endregion
        #region Events
        public event Action<int> OnTurnStart;
        public event Action<int> OnTurnTimeout;
        public event Action<float> OnTimerUpdate;
        #endregion
        #region Unity Lifecycle
        private void Awake()
        {
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
                if (PhotonNetwork.IsMasterClient)
                {
                    if (GameManager.Instance != null && GameManager.Instance.NetworkGameManager != null)
                    {
                        GameManager.Instance.NetworkGameManager.SendEndTurn();
                    }
                    else
                    {
                        NextTurn(); 
                    }
                }
            }
        }
        #endregion
        #region Initialization
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
            SyncTurnToRoomProperties();
            OnTurnStart?.Invoke(_playerActorNumbers[_currentTurnIndex]);
        }
        #endregion
        #region Turn Operations
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
            SyncTurnToRoomProperties();
            OnTurnStart?.Invoke(nextActor);
            if (GameManager.Instance != null)
                GameManager.Instance.NotifyTurnChanged(nextActor);
        }
        public int GetCurrentPlayer()
        {
            if (_playerActorNumbers == null || _playerActorNumbers.Length == 0)
                return -1;
            return _playerActorNumbers[_currentTurnIndex];
        }
        public int[] GetPlayerActorNumbers()
        {
            return _playerActorNumbers;
        }
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
        public bool IsLocalPlayerTurn()
        {
            if (!PhotonNetwork.IsConnected || PhotonNetwork.LocalPlayer == null)
                return false;
            int currentTurnPlayer = GetCurrentPlayer();
            int myActor = PhotonNetwork.LocalPlayer.ActorNumber;
            return currentTurnPlayer == myActor;
        }
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
