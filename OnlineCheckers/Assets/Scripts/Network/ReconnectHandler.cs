using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Checkers.Core;
using Checkers.UI;
using Checkers.Utilities;
namespace Checkers.Network
{
    public class ReconnectHandler : MonoBehaviourPunCallbacks
    {
        #region Fields
        private bool _isReconnecting;
        private int _reconnectAttempts;
        private Coroutine _reconnectCoroutine;
        #endregion
        #region Photon Callbacks
        public override void OnDisconnected(DisconnectCause cause)
        {
            GameLogger.Log(GameLogger.LogLevel.WARN, $"Disconnected from Photon. Cause: {cause}");
            if (cause == DisconnectCause.DisconnectByClientLogic ||
                cause == DisconnectCause.ApplicationQuit)
            {
                GameLogger.Log(GameLogger.LogLevel.INFO,
                    "Disconnect was intentional. Not attempting reconnect.");
                return;
            }
            if (_isReconnecting)
            {
                GameLogger.Log(GameLogger.LogLevel.WARN, "Already attempting reconnect.");
                return;
            }
            _reconnectCoroutine = StartCoroutine(ReconnectRoutine());
        }
        public override void OnJoinedRoom()
        {
            if (_isReconnecting)
            {
                _isReconnecting = false;
                _reconnectAttempts = 0;
                GameLogger.Log(GameLogger.LogLevel.INFO, "Reconnect successful! Restoring game state...");
                if (UIManager.Instance != null)
                    UIManager.Instance.ShowReconnecting(false);
                RestoreGameState();
            }
        }
        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            if (_isReconnecting)
            {
                GameLogger.Log(GameLogger.LogLevel.WARN,
                    $"Reconnect rejoin failed ({returnCode}): {message}");
            }
        }
        #endregion
        #region Reconnection Logic
        private IEnumerator ReconnectRoutine()
        {
            _isReconnecting = true;
            _reconnectAttempts = 0;
            if (UIManager.Instance != null)
                UIManager.Instance.ShowReconnecting(true);
            GameLogger.Log(GameLogger.LogLevel.INFO, "Starting reconnection routine...");
            yield return new WaitForSeconds(2f);
            while (_reconnectAttempts < Constants.MAX_RECONNECT_ATTEMPTS)
            {
                _reconnectAttempts++;
                GameLogger.Log(GameLogger.LogLevel.INFO,
                    $"Reconnect attempt {_reconnectAttempts}/{Constants.MAX_RECONNECT_ATTEMPTS}...");
                bool attemptStarted = PhotonNetwork.ReconnectAndRejoin();
                if (!attemptStarted)
                {
                    GameLogger.Log(GameLogger.LogLevel.WARN,
                        $"ReconnectAndRejoin returned false. Trying Reconnect...");
                    attemptStarted = PhotonNetwork.Reconnect();
                }
                if (!attemptStarted)
                {
                    GameLogger.Log(GameLogger.LogLevel.WARN,
                        $"Reconnect also returned false. Attempt {_reconnectAttempts} failed.");
                }
                float waitTime = Constants.RECONNECT_DELAY_SECONDS;
                float elapsed = 0f;
                while (elapsed < waitTime)
                {
                    if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
                    {
                        GameLogger.Log(GameLogger.LogLevel.INFO, "Reconnected successfully during wait.");
                        _isReconnecting = false;
                        yield break;
                    }
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
                {
                    _isReconnecting = false;
                    yield break;
                }
            }
            _isReconnecting = false;
            GameLogger.Log(GameLogger.LogLevel.ERROR,
                $"Reconnection failed after {Constants.MAX_RECONNECT_ATTEMPTS} attempts.");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowReconnecting(false);
                UIManager.Instance.ShowConnectionFailed();
            }
            ReturnToLobby();
        }
        private void RestoreGameState()
        {
            if (!PhotonNetwork.InRoom)
            {
                GameLogger.Log(GameLogger.LogLevel.ERROR, "RestoreGameState called but not in a room.");
                return;
            }
            object stateObj;
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(
                Constants.ROOM_PROP_GAME_STATE, out stateObj))
            {
                string serializedState = stateObj as string;
                if (!string.IsNullOrEmpty(serializedState))
                {
                    GameLogger.Log(GameLogger.LogLevel.INFO,
                        $"Restoring state from room properties. Length: {serializedState.Length}");
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.RestoreStateFromNetwork(serializedState);
                    }
                    else
                    {
                        GameLogger.Log(GameLogger.LogLevel.ERROR,
                            "GameManager.Instance is null. Cannot restore state.");
                    }
                }
                else
                {
                    GameLogger.Log(GameLogger.LogLevel.WARN,
                        "Room property BOARD_STATE is empty. Game may not have started.");
                }
            }
            else
            {
                GameLogger.Log(GameLogger.LogLevel.WARN,
                    "Room property BOARD_STATE not found.");
            }
        }
        private void ReturnToLobby()
        {
            GameLogger.Log(GameLogger.LogLevel.INFO, "Returning to lobby scene...");
            if (PhotonNetwork.IsConnected)
                PhotonNetwork.Disconnect();
            UnityEngine.SceneManagement.SceneManager.LoadScene(Constants.SCENE_LOBBY);
        }
        public void CancelReconnect()
        {
            if (_reconnectCoroutine != null)
            {
                StopCoroutine(_reconnectCoroutine);
                _reconnectCoroutine = null;
            }
            _isReconnecting = false;
            if (UIManager.Instance != null)
                UIManager.Instance.ShowReconnecting(false);
            ReturnToLobby();
        }
        #endregion
    }
}
