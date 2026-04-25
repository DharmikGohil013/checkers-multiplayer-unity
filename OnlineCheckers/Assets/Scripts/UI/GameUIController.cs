using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using Checkers.Core;
using Checkers.Network;
using Checkers.Utilities;

namespace Checkers.UI
{
    /// <summary>
    /// Controls the in-game UI: turn panel, timer bar, score panel, resign button, and game over panel.
    /// Subscribes to GameManager and TurnManager events.
    /// </summary>
    public class GameUIController : MonoBehaviour
    {
        #region UI References

        [Header("Turn Panel")]
        [SerializeField] private TextMeshProUGUI currentPlayerText;
        [SerializeField] private Image turnColorImage;

        [Header("Timer")]
        [SerializeField] private Slider timerSlider;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private GameObject timerPanel;

        [Header("Score Panel")]
        [SerializeField] private TextMeshProUGUI[] playerScoreTexts; // One per player slot
        [SerializeField] private Image[] playerColorImages;          // Color indicators

        [Header("Action Buttons")]
        [SerializeField] private Button resignButton;

        [Header("Game Over Panel")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI winnerNameText;
        [SerializeField] private Image winnerColorImage;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button quitButton;

        [Header("Confirmation Dialog")]
        [SerializeField] private GameObject resignConfirmPanel;
        [SerializeField] private Button resignConfirmYes;
        [SerializeField] private Button resignConfirmNo;

        #endregion

        #region Fields

        private GameManager _gameManager;
        private TurnManager _turnManager;
        private BoardManager _boardManager;
        private Data.PieceConfig _pieceConfig;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _gameManager = GameManager.Instance;
            if (_gameManager != null)
            {
                _turnManager = _gameManager.TurnManager;
                _boardManager = _gameManager.BoardManager;
                _pieceConfig = _gameManager.PieceConfig;

                // Subscribe to events
                _gameManager.OnTurnChanged += HandleTurnChanged;
                _gameManager.OnGameEnd += HandleGameEnd;
                _gameManager.OnGameStart += HandleGameStart;
            }

            if (_turnManager != null)
            {
                _turnManager.OnTimerUpdate += HandleTimerUpdate;
            }

            SetupButtons();
            HideGameOver();

            if (resignConfirmPanel != null)
                resignConfirmPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_gameManager != null)
            {
                _gameManager.OnTurnChanged -= HandleTurnChanged;
                _gameManager.OnGameEnd -= HandleGameEnd;
                _gameManager.OnGameStart -= HandleGameStart;
            }

            if (_turnManager != null)
            {
                _turnManager.OnTimerUpdate -= HandleTimerUpdate;
            }

            CleanupButtons();
        }

        #endregion

        #region Setup

        private void SetupButtons()
        {
            if (resignButton != null)
            {
                resignButton.onClick.AddListener(OnResignClicked);
                resignButton.interactable = true;
            }
            else
            {
                GameLogger.Log(GameLogger.LogLevel.WARN, "Resign button is NOT assigned in GameUIController!");
            }

            if (playAgainButton != null)
                playAgainButton.onClick.AddListener(OnPlayAgainClicked);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            if (resignConfirmYes != null)
                resignConfirmYes.onClick.AddListener(OnResignConfirmed);

            if (resignConfirmNo != null)
                resignConfirmNo.onClick.AddListener(OnResignCancelled);
        }

        private void CleanupButtons()
        {
            if (resignButton != null)
                resignButton.onClick.RemoveListener(OnResignClicked);

            if (playAgainButton != null)
                playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);

            if (quitButton != null)
                quitButton.onClick.RemoveListener(OnQuitClicked);

            if (resignConfirmYes != null)
                resignConfirmYes.onClick.RemoveListener(OnResignConfirmed);

            if (resignConfirmNo != null)
                resignConfirmNo.onClick.RemoveListener(OnResignCancelled);
        }

        #endregion

        #region Event Handlers

        private void HandleGameStart()
        {
            HideGameOver();
            
            if (resignButton != null)
                resignButton.interactable = true;

            // Setup timer visibility
            bool hasTimer = _gameManager != null && _gameManager.GameSettings != null 
                && _gameManager.GameSettings.turnTimeLimit > 0f;
            if (timerPanel != null)
                timerPanel.SetActive(hasTimer);

            if (timerSlider != null)
            {
                timerSlider.minValue = 0f;
                timerSlider.maxValue = 1f;
                timerSlider.value = 1f;
            }

            // Initialize player score displays
            InitializeScorePanel();

            GameLogger.Log(GameLogger.LogLevel.INFO, "Game UI initialized and ready.");
        }

        private void HandleTurnChanged(int actorNumber)
        {
            // Lazy load pieceConfig if it wasn't available at Start
            if (_pieceConfig == null && _gameManager != null)
                _pieceConfig = _gameManager.PieceConfig;

            // Also re-cache references if they were lost
            if (_turnManager == null && _gameManager != null)
                _turnManager = _gameManager.TurnManager;
            if (_boardManager == null && _gameManager != null)
                _boardManager = _gameManager.BoardManager;

            // Update turn indicator
            Player currentPlayer = GetPlayerByActorNumber(actorNumber);
            string playerName = currentPlayer != null ? currentPlayer.NickName : $"Player {actorNumber}";

            int playerIndex = GetPlayerIndex(actorNumber);
            Color playerColor = _pieceConfig != null ? _pieceConfig.GetPlayerColor(playerIndex) : Color.white;

            if (currentPlayerText != null)
            {
                string turnPrefix = (_turnManager != null && _turnManager.IsLocalPlayerTurn()) 
                    ? "Your Turn" 
                    : $"{playerName}'s Turn";
                currentPlayerText.text = turnPrefix;
            }

            if (turnColorImage != null)
                turnColorImage.color = playerColor;

            // Update piece counts
            UpdatePieceCounts();

            // Update UIManager as well
            if (UIManager.Instance != null)
                UIManager.Instance.UpdateTurnIndicator(playerName, playerColor);
        }

        private void HandleTimerUpdate(float timeRemaining)
        {
            if (timerSlider != null)
            {
                float maxTime = 30f; // Default fallback
                if (_gameManager != null && _gameManager.GameSettings != null)
                {
                    maxTime = _gameManager.GameSettings.turnTimeLimit;
                }
                
                // Force limits in case they were changed in the inspector
                timerSlider.minValue = 0f;
                timerSlider.maxValue = 1f;
                
                timerSlider.value = maxTime > 0f ? timeRemaining / maxTime : 0f;
            }

            if (timerText != null)
            {
                int seconds = Mathf.CeilToInt(timeRemaining);
                timerText.text = seconds.ToString();

                // Color coding
                if (timeRemaining <= 5f)
                    timerText.color = Color.red;
                else if (timeRemaining <= 10f)
                    timerText.color = new Color(1f, 0.65f, 0f);
                else
                    timerText.color = Color.white;
            }

            // Update UIManager timer display
            if (UIManager.Instance != null)
                UIManager.Instance.UpdateTimerDisplay(timeRemaining);
        }

        private void HandleGameEnd(int winnerActorNumber)
        {
            ShowGameOver(winnerActorNumber);
        }

        #endregion

        #region Button Handlers
        
        private void OnResignClicked()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowResignConfirmation();
            }
            else
            {
                // Fallback if UIManager is missing
                GameLogger.Log(GameLogger.LogLevel.WARN, "UIManager.Instance not found! Falling back to local resign.");
                OnResignConfirmed();
            }
        }

        private void OnResignConfirmed()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ConfirmResign();
            }
        }

        private void OnResignCancelled()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.CancelResign();
            }
        }

        private void OnPlayAgainClicked()
        {
            GameLogger.Log(GameLogger.LogLevel.INFO, "Play Again clicked. Returning to lobby.");

            if (PhotonNetwork.InRoom)
                PhotonNetwork.LeaveRoom();

            UnityEngine.SceneManagement.SceneManager.LoadScene(Constants.SCENE_LOBBY);
        }

        private void OnQuitClicked()
        {
            GameLogger.Log(GameLogger.LogLevel.INFO, "Quit clicked.");

            if (PhotonNetwork.IsConnected)
                PhotonNetwork.Disconnect();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () => 
            {
                UnityEditor.EditorApplication.isPlaying = false;
            };
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Score Panel

        private void InitializeScorePanel()
        {
            if (_turnManager == null || _pieceConfig == null)
                return;

            int[] playerActors = _turnManager.GetPlayerActorNumbers();
            if (playerActors == null)
                return;

            for (int i = 0; i < playerActors.Length && i < 4; i++)
            {
                // Set player color indicator
                if (playerColorImages != null && i < playerColorImages.Length && playerColorImages[i] != null)
                {
                    playerColorImages[i].color = _pieceConfig.GetPlayerColor(i);
                    playerColorImages[i].gameObject.SetActive(true);
                }

                // Set initial piece count
                if (playerScoreTexts != null && i < playerScoreTexts.Length && playerScoreTexts[i] != null)
                {
                    int count = _boardManager != null ? _boardManager.GetPieceCount(i + 1) : 0;
                    playerScoreTexts[i].text = count.ToString();
                    playerScoreTexts[i].gameObject.SetActive(true);
                }
            }

            // Hide unused player slots
            for (int i = playerActors.Length; i < 4; i++)
            {
                if (playerColorImages != null && i < playerColorImages.Length && playerColorImages[i] != null)
                    playerColorImages[i].gameObject.SetActive(false);

                if (playerScoreTexts != null && i < playerScoreTexts.Length && playerScoreTexts[i] != null)
                    playerScoreTexts[i].gameObject.SetActive(false);
            }
        }

        private void UpdatePieceCounts()
        {
            if (_boardManager == null || _turnManager == null)
                return;

            int[] playerActors = _turnManager.GetPlayerActorNumbers();
            if (playerActors == null)
                return;

            for (int i = 0; i < playerActors.Length && i < 4; i++)
            {
                int count = _boardManager.GetPieceCount(i + 1);

                if (playerScoreTexts != null && i < playerScoreTexts.Length && playerScoreTexts[i] != null)
                    playerScoreTexts[i].text = count.ToString();

                if (UIManager.Instance != null)
                    UIManager.Instance.UpdatePieceCount(i, count);
            }
        }

        #endregion

        #region Game Over

        private void ShowGameOver(int winnerActorNumber)
        {
            GameLogger.Log(GameLogger.LogLevel.INFO, $"GameUIController: Handling Game Over for Actor {winnerActorNumber}");

            if (resignButton != null)
                resignButton.interactable = false;

            // Lazy load pieceConfig
            if (_pieceConfig == null && _gameManager != null)
                _pieceConfig = _gameManager.PieceConfig;

            Player winner = GetPlayerByActorNumber(winnerActorNumber);
            string winnerName = winner != null ? winner.NickName : $"Player {winnerActorNumber}";

            int winnerIndex = GetPlayerIndex(winnerActorNumber);
            Color winnerColor = _pieceConfig != null ? _pieceConfig.GetPlayerColor(winnerIndex) : Color.white;

            if (winnerNameText != null)
                winnerNameText.text = $"{winnerName} Wins!";

            if (winnerColorImage != null)
                winnerColorImage.color = winnerColor;

            // Always activate the local gameOverPanel directly
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                gameOverPanel.transform.SetAsLastSibling();
            }

            // Also delegate to UIManager
            if (UIManager.Instance != null)
                UIManager.Instance.ShowGameOver(winnerName, winnerColor);
        }

        private void HideGameOver()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
            
            if (resignButton != null)
                resignButton.interactable = true;
        }

        #endregion

        #region Helpers

        private Player GetPlayerByActorNumber(int actorNumber)
        {
            if (!PhotonNetwork.IsConnected)
                return null;

            Player[] players = PhotonNetwork.PlayerList;
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].ActorNumber == actorNumber)
                    return players[i];
            }
            return null;
        }

        private int GetPlayerIndex(int actorNumber)
        {
            if (_turnManager == null)
                return 0;

            int[] actorNumbers = _turnManager.GetPlayerActorNumbers();
            if (actorNumbers == null)
                return 0;

            for (int i = 0; i < actorNumbers.Length; i++)
            {
                if (actorNumbers[i] == actorNumber)
                    return i;
            }
            return 0;
        }

        #endregion
    }
}
