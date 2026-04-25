using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using Checkers.Utilities;
namespace Checkers.UI
{
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
        [SerializeField] private TextMeshProUGUI[] pieceCountTexts; 
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
            if (_instance != null && _instance != this)
            {
                bool existingIsAlive = !ReferenceEquals(_instance, null) && ((UnityEngine.Object)_instance) != null;
                if (existingIsAlive)
                {
                    Destroy(gameObject);
                    return;
                }
            }
            _instance = this;
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
                if (target == gameOverPanel && gamePanel != null)
                {
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
        public void UpdateTurnIndicator(string playerName, Color playerColor)
        {
            if (turnIndicatorText != null)
                turnIndicatorText.text = $"{playerName}'s Turn";
            if (turnColorIndicator != null)
                turnColorIndicator.color = playerColor;
        }
        #endregion
        #region Timer Display
        public void UpdateTimerDisplay(float timeRemaining)
        {
            if (timerBar != null)
            {
                timerBar.gameObject.SetActive(true);
                float maxTime = 30f; 
                if (Core.GameManager.Instance != null && Core.GameManager.Instance.GameSettings != null)
                    maxTime = Core.GameManager.Instance.GameSettings.turnTimeLimit;
                if (maxTime > 0f)
                {
                    timerBar.minValue = 0f;
                    timerBar.maxValue = 1f;
                    timerBar.value = timeRemaining / maxTime;
                }
            }
            if (timerText != null)
            {
                int seconds = Mathf.CeilToInt(timeRemaining);
                timerText.text = seconds.ToString();
                if (timeRemaining <= 5f)
                    timerText.color = Color.red;
                else if (timeRemaining <= 10f)
                    timerText.color = new Color(1f, 0.65f, 0f); 
                else
                    timerText.color = Color.white;
            }
        }
        public void HideTimer()
        {
            if (timerBar != null)
                timerBar.gameObject.SetActive(false);
            if (timerText != null)
                timerText.gameObject.SetActive(false);
        }
        #endregion
        #region Game Over
        public void ShowGameOver(string winnerName, Color winnerColor)
        {
            GameLogger.Log(GameLogger.LogLevel.INFO, $"UIManager: ShowGameOver for {winnerName}");
            if (gameOverPanel == null)
            {
                GameLogger.Log(GameLogger.LogLevel.ERROR, "UIManager: gameOverPanel is NULL! Cannot show Game Over screen.");
                return;
            }
            gameOverPanel.SetActive(true);
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
            UnityEditor.EditorApplication.delayCall += () => 
            {
                UnityEditor.EditorApplication.isPlaying = false;
            };
#else
            Application.Quit();
#endif
        }
        #endregion
        #region Resign Logic
        public void ShowResignConfirmation()
        {
            if (resignConfirmPanel != null)
            {
                resignConfirmPanel.SetActive(true);
                GameLogger.Log(GameLogger.LogLevel.INFO, "Resign confirmation shown via UIManager.");
            }
        }
        public void ConfirmResign()
        {
            GameLogger.Log(GameLogger.LogLevel.INFO, "Resign confirmed via UIManager.");
            if (resignConfirmPanel != null)
                resignConfirmPanel.SetActive(false);
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
                gm.EndGame(-1);
            }
        }
        public void CancelResign()
        {
            if (resignConfirmPanel != null)
                resignConfirmPanel.SetActive(false);
            GameLogger.Log(GameLogger.LogLevel.INFO, "Resign cancelled via UIManager.");
        }
        #endregion
        #region Reconnecting
        public void ShowReconnecting(bool show)
        {
            SetPanelActive(reconnectingPanel, show);
            if (show && reconnectingText != null)
            {
                reconnectingText.text = "Reconnecting...";
                StartCoroutine(AnimateReconnectingText());
            }
        }
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
        public void UpdatePlayerList(Player[] players)
        {
            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Player list updated: {players.Length} players");
        }
        #endregion
        #region Piece Count
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
