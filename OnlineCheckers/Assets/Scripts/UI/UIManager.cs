using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using Checkers.Utilities;

namespace Checkers.UI
{
    /// <summary>
    /// Singleton UIManager that controls all UI panels in the game.
    /// Manages panel visibility, turn indicators, timer displays, and game over screens.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        #region Singleton

        private static UIManager _instance;

        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameLogger.Log(GameLogger.LogLevel.WARN, "UIManager instance is null.");
                }
                return _instance;
            }
        }

        #endregion

        #region Panel References

        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject reconnectingPanel;
        [SerializeField] private GameObject connectionFailedPanel;

        [Header("Game UI Elements")]
        [SerializeField] private TextMeshProUGUI turnIndicatorText;
        [SerializeField] private Image turnColorIndicator;
        [SerializeField] private Slider timerBar;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI[] pieceCountTexts; // One per player

        [Header("Game Over UI")]
        [SerializeField] private TextMeshProUGUI winnerText;
        [SerializeField] private Image winnerColorIndicator;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button quitButton;

        [Header("Reconnecting UI")]
        [SerializeField] private TextMeshProUGUI reconnectingText;
        [SerializeField] private TextMeshProUGUI connectionFailedText;

        [Header("Confirmation Dialog")]
        [SerializeField] private GameObject resignConfirmPanel;
        [SerializeField] private Button resignConfirmYes;
        [SerializeField] private Button resignConfirmNo;

        [Header("Flash Effect")]
        [SerializeField] private Image invalidMoveFlashImage;

        #endregion

        #region Fields

        private Coroutine _flashCoroutine;
        private GameObject[] _allPanels;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Handle singleton — same robust pattern as GameManager
            if (_instance != null && _instance != this)
            {
                bool existingIsAlive = !ReferenceEquals(_instance, null) && ((UnityEngine.Object)_instance) != null;

                if (existingIsAlive)
                {
                    // True duplicate in the same scene — destroy this one only
                    Destroy(gameObject);
                    return;
                }
                // else: stale reference from previous scene — replace it
            }

            _instance = this;

            // Cache all panels for easy management
            _allPanels = new GameObject[]
            {
                mainMenuPanel,
                lobbyPanel,
                gamePanel,
                gameOverPanel,
                reconnectingPanel,
                connectionFailedPanel,
                resignConfirmPanel
            };

            SetupResignButtons();
        }

        private void SetupResignButtons()
        {
            if (resignConfirmYes != null)
                resignConfirmYes.onClick.AddListener(ConfirmResign);

            if (resignConfirmNo != null)
                resignConfirmNo.onClick.AddListener(CancelResign);

            if (playAgainButton != null)
                playAgainButton.onClick.AddListener(OnPlayAgainClicked);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        #endregion

        #region Panel Management

        /// <summary>
        /// Shows a specific panel by name, hiding all others.
        /// Valid names: "MainMenu", "Lobby", "Game", "GameOver", "Reconnecting", "ConnectionFailed"
        /// </summary>
        public void ShowPanel(string panelName)
        {
            GameLogger.Log(GameLogger.LogLevel.INFO, $"UIManager: Showing panel '{panelName}'");
            
            HideAllPanels();

            GameObject target = null;
            switch (panelName)
            {
                case "MainMenu": target = mainMenuPanel; break;
                case "Lobby": target = lobbyPanel; break;
                case "Game": target = gamePanel; break;
                case "GameOver": target = gameOverPanel; break;
                case "Reconnecting": target = reconnectingPanel; break;
                case "ConnectionFailed": target = connectionFailedPanel; break;
            }

            if (target != null)
            {
                SetPanelActive(target, true);
                
                // Safety check: if target is a child of another managed panel, we might have just hidden its parent!
                // We should ensure the parent is active if it's the gamePanel or lobbyPanel.
                if (target == gameOverPanel && gamePanel != null)
                {
                    // Many developers put GameOver as a child of the Game panel.
                    // If so, we need the Game panel active to see the GameOver panel.
                    if (target.transform.IsChildOf(gamePanel.transform))
                    {
                        GameLogger.Log(GameLogger.LogLevel.INFO, "GameOver is child of GamePanel. Activating parent.");
                        SetPanelActive(gamePanel, true);
                    }
                }
            }
            else
            {
                GameLogger.Log(GameLogger.LogLevel.WARN, $"UIManager: Panel '{panelName}' reference is null!");
            }
        }

        private void HideAllPanels()
        {
            if (_allPanels == null)
                return;

            for (int i = 0; i < _allPanels.Length; i++)
            {
                SetPanelActive(_allPanels[i], false);
            }
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
                panel.SetActive(active);
        }

        #endregion

        #region Turn Indicator

        /// <summary>
        /// Updates the turn indicator to show the current player's name and color.
        /// </summary>
        public void UpdateTurnIndicator(string playerName, Color playerColor)
        {
            if (turnIndicatorText != null)
                turnIndicatorText.text = $"{playerName}'s Turn";

            if (turnColorIndicator != null)
                turnColorIndicator.color = playerColor;
        }

        #endregion

        #region Timer Display

        /// <summary>
        /// Updates the timer bar and text display.
        /// </summary>
        /// <param name="timeRemaining">Remaining time in seconds.</param>
        public void UpdateTimerDisplay(float timeRemaining)
        {
            if (timerBar != null)
            {
                // Assuming max time is set via GameSettings
                timerBar.gameObject.SetActive(true);
                // Normalize based on GameSettings if available
                float maxTime = 30f; // Fallback
                if (Core.GameManager.Instance != null && Core.GameManager.Instance.GameSettings != null)
                    maxTime = Core.GameManager.Instance.GameSettings.turnTimeLimit;

                if (maxTime > 0f)
                    timerBar.value = timeRemaining / maxTime;
            }

            if (timerText != null)
            {
                int seconds = Mathf.CeilToInt(timeRemaining);
                timerText.text = seconds.ToString();

                // Change color when low
                if (timeRemaining <= 5f)
                    timerText.color = Color.red;
                else if (timeRemaining <= 10f)
                    timerText.color = new Color(1f, 0.65f, 0f); // Orange
                else
                    timerText.color = Color.white;
            }
        }

        /// <summary>
        /// Hides the timer display (for untimed games).
        /// </summary>
        public void HideTimer()
        {
            if (timerBar != null)
                timerBar.gameObject.SetActive(false);

            if (timerText != null)
                timerText.gameObject.SetActive(false);
        }

        #endregion

        #region Game Over

        /// <summary>
        /// Shows the game over panel with the winner's information.
        /// </summary>
        public void ShowGameOver(string winnerName, Color winnerColor)
        {
            GameLogger.Log(GameLogger.LogLevel.INFO, $"UIManager: ShowGameOver for {winnerName}");
            
            if (gameOverPanel == null)
            {
                GameLogger.Log(GameLogger.LogLevel.ERROR, "UIManager: gameOverPanel is NULL! Cannot show Game Over screen.");
                return;
            }

            // DO NOT call ShowPanel() here — it hides all panels first,
            // which would hide the game panel (potentially the parent of gameOverPanel).
            // Instead, just activate the gameOverPanel directly on top of the game.
            gameOverPanel.SetActive(true);
            
            // Ensure it's on top of everything
            gameOverPanel.transform.SetAsLastSibling();

            if (winnerText != null)
                winnerText.text = $"{winnerName} Wins!";
            else
                GameLogger.Log(GameLogger.LogLevel.WARN, "UIManager: winnerText is NOT assigned!");

            if (winnerColorIndicator != null)
                winnerColorIndicator.color = winnerColor;

            GameLogger.Log(GameLogger.LogLevel.INFO, $"Game Over UI shown. Winner: {winnerName}");
        }

        private void OnPlayAgainClicked()
        {
            GameLogger.Log(GameLogger.LogLevel.INFO, "UIManager: Play Again clicked. Returning to lobby.");

            if (PhotonNetwork.InRoom)
                PhotonNetwork.LeaveRoom();

            UnityEngine.SceneManagement.SceneManager.LoadScene(Constants.SCENE_LOBBY);
        }

        private void OnQuitClicked()
        {
            GameLogger.Log(GameLogger.LogLevel.INFO, "UIManager: Quit clicked.");

            if (PhotonNetwork.IsConnected)
                PhotonNetwork.Disconnect();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Resign Logic

        /// <summary>
        /// Shows the resign confirmation dialog.
        /// </summary>
        public void ShowResignConfirmation()
        {
            if (resignConfirmPanel != null)
            {
                resignConfirmPanel.SetActive(true);
                GameLogger.Log(GameLogger.LogLevel.INFO, "Resign confirmation shown via UIManager.");
            }
        }

        /// <summary>
        /// Executes the resignation logic. Called when the player confirms resignation.
        /// </summary>
        public void ConfirmResign()
        {
            GameLogger.Log(GameLogger.LogLevel.INFO, "Resign confirmed via UIManager.");

            if (resignConfirmPanel != null)
                resignConfirmPanel.SetActive(false);

            // Access Managers to execute game over
            Core.GameManager gm = Core.GameManager.Instance;
            if (gm == null) return;

            Core.TurnManager tm = gm.TurnManager;
            if (tm != null && PhotonNetwork.IsConnectedAndReady)
            {
                int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
                int[] players = tm.GetPlayerActorNumbers();

                int winnerActor = -1;
                if (players != null && players.Length > 0)
                {
                    foreach (int actor in players)
                    {
                        if (actor != localActor)
                        {
                            winnerActor = actor;
                            break;
                        }
                    }
                    if (winnerActor == -1) winnerActor = localActor;
                }

                if (gm.NetworkGameManager != null)
                {
                    gm.NetworkGameManager.SendGameOver(winnerActor);
                }
            }
            else
            {
                // Fallback for local/offline
                gm.EndGame(-1);
            }
        }

        /// <summary>
        /// Cancels the resignation attempt.
        /// </summary>
        public void CancelResign()
        {
            if (resignConfirmPanel != null)
                resignConfirmPanel.SetActive(false);
            
            GameLogger.Log(GameLogger.LogLevel.INFO, "Resign cancelled via UIManager.");
        }

        #endregion

        #region Reconnecting

        /// <summary>
        /// Shows or hides the reconnecting overlay.
        /// </summary>
        public void ShowReconnecting(bool show)
        {
            SetPanelActive(reconnectingPanel, show);

            if (show && reconnectingText != null)
            {
                reconnectingText.text = "Reconnecting...";
                StartCoroutine(AnimateReconnectingText());
            }
        }

        /// <summary>
        /// Shows the connection failed panel.
        /// </summary>
        public void ShowConnectionFailed()
        {
            SetPanelActive(reconnectingPanel, false);
            SetPanelActive(connectionFailedPanel, true);

            if (connectionFailedText != null)
                connectionFailedText.text = "Connection Failed\nReturning to lobby...";
        }

        private IEnumerator AnimateReconnectingText()
        {
            int dots = 0;
            while (reconnectingPanel != null && reconnectingPanel.activeSelf)
            {
                dots = (dots + 1) % 4;
                string dotStr = new string('.', dots);
                if (reconnectingText != null)
                    reconnectingText.text = $"Reconnecting{dotStr}";

                yield return new WaitForSeconds(0.5f);
            }
        }

        #endregion

        #region Player List

        /// <summary>
        /// Updates the player list display with current room players.
        /// </summary>
        public void UpdatePlayerList(Player[] players)
        {
            // Implementation depends on UI layout.
            // This provides the data — LobbyUIController handles specific list items.
            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Player list updated: {players.Length} players");
        }

        #endregion

        #region Piece Count

        /// <summary>
        /// Updates the piece count display for a specific player.
        /// </summary>
        public void UpdatePieceCount(int playerIndex, int count)
        {
            if (pieceCountTexts != null && playerIndex >= 0 && playerIndex < pieceCountTexts.Length)
            {
                if (pieceCountTexts[playerIndex] != null)
                    pieceCountTexts[playerIndex].text = count.ToString();
            }
        }

        #endregion

        #region Invalid Move Flash

        /// <summary>
        /// Shows a brief red flash to indicate an invalid move attempt.
        /// </summary>
        public void ShowInvalidMoveFlash()
        {
            if (invalidMoveFlashImage == null)
                return;

            if (_flashCoroutine != null)
                StopCoroutine(_flashCoroutine);

            _flashCoroutine = StartCoroutine(FlashCoroutine());
        }

        private IEnumerator FlashCoroutine()
        {
            if (invalidMoveFlashImage == null)
                yield break;

            Color flashColor = new Color(1f, 0f, 0f, 0.3f);
            invalidMoveFlashImage.color = flashColor;
            invalidMoveFlashImage.gameObject.SetActive(true);

            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0.3f, 0f, elapsed / duration);
                invalidMoveFlashImage.color = new Color(1f, 0f, 0f, alpha);
                yield return null;
            }

            invalidMoveFlashImage.gameObject.SetActive(false);
            _flashCoroutine = null;
        }

        #endregion
    }
}
