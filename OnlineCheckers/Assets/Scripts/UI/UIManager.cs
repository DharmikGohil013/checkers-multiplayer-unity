using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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

        [Header("Reconnecting UI")]
        [SerializeField] private TextMeshProUGUI reconnectingText;
        [SerializeField] private TextMeshProUGUI connectionFailedText;

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
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Cache all panels for easy management
            _allPanels = new GameObject[]
            {
                mainMenuPanel,
                lobbyPanel,
                gamePanel,
                gameOverPanel,
                reconnectingPanel,
                connectionFailedPanel
            };
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
            HideAllPanels();

            switch (panelName)
            {
                case "MainMenu":
                    SetPanelActive(mainMenuPanel, true);
                    break;
                case "Lobby":
                    SetPanelActive(lobbyPanel, true);
                    break;
                case "Game":
                    SetPanelActive(gamePanel, true);
                    break;
                case "GameOver":
                    SetPanelActive(gameOverPanel, true);
                    break;
                case "Reconnecting":
                    SetPanelActive(reconnectingPanel, true);
                    break;
                case "ConnectionFailed":
                    SetPanelActive(connectionFailedPanel, true);
                    break;
                default:
                    GameLogger.Log(GameLogger.LogLevel.WARN, $"Unknown panel name: {panelName}");
                    break;
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
            ShowPanel("GameOver");

            if (winnerText != null)
                winnerText.text = $"{winnerName} Wins!";

            if (winnerColorIndicator != null)
                winnerColorIndicator.color = winnerColor;

            GameLogger.Log(GameLogger.LogLevel.INFO, $"Game Over UI shown. Winner: {winnerName}");
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
