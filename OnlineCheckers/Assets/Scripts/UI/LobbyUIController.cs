using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using Checkers.Network;
using Checkers.Utilities;

namespace Checkers.UI
{
    /// <summary>
    /// Controls the Lobby UI: room name input, player count selection, board size selection,
    /// player list display, and room creation/joining buttons.
    /// </summary>
    public class LobbyUIController : MonoBehaviour
    {
        #region UI References

        [Header("Input Fields")]
        [SerializeField] private TMP_InputField roomNameInput;
        [SerializeField] private TMP_InputField playerNameInput;

        [Header("Dropdowns")]
        [SerializeField] private TMP_Dropdown playerCountDropdown;
        [SerializeField] private TMP_Dropdown boardSizeDropdown;

        [Header("Buttons")]
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private Button joinRandomButton;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button disconnectButton;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Player List")]
        [SerializeField] private Transform playerListParent;
        [SerializeField] private GameObject playerListItemPrefab;

        [Header("Room Info")]
        [SerializeField] private TextMeshProUGUI roomInfoText;
        [SerializeField] private GameObject roomInfoPanel;
        [SerializeField] private GameObject joinPanel;

        [Header("Network")]
        [SerializeField] private NetworkLobbyManager networkLobbyManager;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // Cache network manager
            if (networkLobbyManager == null)
                networkLobbyManager = FindAnyObjectByType<NetworkLobbyManager>();

            SetupDropdowns();
            SetupButtons();
            SetStartButtonVisible(false);

            // Show join panel, hide room info
            if (roomInfoPanel != null)
                roomInfoPanel.SetActive(false);
            if (joinPanel != null)
                joinPanel.SetActive(true);

            // Set default player name
            if (playerNameInput != null)
            {
                string savedName = PlayerPrefs.GetString("PlayerName", "Player" + Random.Range(100, 999));
                playerNameInput.text = savedName;
                PhotonNetwork.NickName = savedName;
            }
        }

        #endregion

        #region Setup

        private void SetupDropdowns()
        {
            // Player Count Dropdown: 2 or 4 players
            if (playerCountDropdown != null)
            {
                playerCountDropdown.ClearOptions();
                playerCountDropdown.AddOptions(new System.Collections.Generic.List<string> { "2 Players", "4 Players" });
                playerCountDropdown.value = 0;
                playerCountDropdown.onValueChanged.AddListener(OnPlayerCountChanged);
            }

            // Board Size Dropdown: 4x4, 6x6, 8x8
            if (boardSizeDropdown != null)
            {
                boardSizeDropdown.ClearOptions();
                boardSizeDropdown.AddOptions(new System.Collections.Generic.List<string> { "4x4", "6x6", "8x8" });
                boardSizeDropdown.value = 2; // Default to 8x8
                boardSizeDropdown.onValueChanged.AddListener(OnBoardSizeChanged);
            }
        }

        private void SetupButtons()
        {
            if (createRoomButton != null)
                createRoomButton.onClick.AddListener(OnCreateRoomClicked);

            if (joinRoomButton != null)
                joinRoomButton.onClick.AddListener(OnJoinRoomClicked);

            if (joinRandomButton != null)
                joinRandomButton.onClick.AddListener(OnJoinRandomClicked);

            if (startGameButton != null)
                startGameButton.onClick.AddListener(OnStartGameClicked);

            if (disconnectButton != null)
                disconnectButton.onClick.AddListener(OnDisconnectClicked);
        }

        #endregion

        #region Button Handlers

        private void OnCreateRoomClicked()
        {
            UpdatePlayerName();
            string roomName = roomNameInput != null ? roomNameInput.text : "";
            networkLobbyManager?.CreateRoom(roomName);
        }

        private void OnJoinRoomClicked()
        {
            UpdatePlayerName();
            string roomName = roomNameInput != null ? roomNameInput.text : "";
            networkLobbyManager?.JoinRoom(roomName);
        }

        private void OnJoinRandomClicked()
        {
            UpdatePlayerName();
            networkLobbyManager?.JoinRandomRoom();
        }

        private void OnStartGameClicked()
        {
            networkLobbyManager?.StartGame();
        }

        private void OnDisconnectClicked()
        {
            networkLobbyManager?.Disconnect();

            // Reset UI
            if (roomInfoPanel != null)
                roomInfoPanel.SetActive(false);
            if (joinPanel != null)
                joinPanel.SetActive(true);

            SetStartButtonVisible(false);
            SetStatusText("Disconnected.");
        }

        private void OnPlayerCountChanged(int index)
        {
            int count = index == 0 ? 2 : 4;
            networkLobbyManager?.SetPlayerCount(count);

            GameLogger.Log(GameLogger.LogLevel.INFO, $"Player count set to: {count}");
        }

        private void OnBoardSizeChanged(int index)
        {
            int[] sizes = { 4, 6, 8 };
            int size = sizes[Mathf.Clamp(index, 0, sizes.Length - 1)];
            networkLobbyManager?.SetBoardSize(size);

            GameLogger.Log(GameLogger.LogLevel.INFO, $"Board size set to: {size}x{size}");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Called when the local player joins a room. Updates UI to show room info.
        /// </summary>
        public void OnJoinedRoom()
        {
            if (joinPanel != null)
                joinPanel.SetActive(false);

            if (roomInfoPanel != null)
                roomInfoPanel.SetActive(true);

            if (roomInfoText != null)
            {
                roomInfoText.text = $"Room: {PhotonNetwork.CurrentRoom.Name}\n" +
                                   $"Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}";
            }
        }

        /// <summary>
        /// Updates the player list display with current room players.
        /// </summary>
        public void UpdatePlayerList(Player[] players)
        {
            if (playerListParent == null)
                return;

            // Clear existing items
            for (int i = playerListParent.childCount - 1; i >= 0; i--)
            {
                Destroy(playerListParent.GetChild(i).gameObject);
            }

            // Create new items
            for (int i = 0; i < players.Length; i++)
            {
                Player player = players[i];

                if (playerListItemPrefab != null)
                {
                    GameObject item = Instantiate(playerListItemPrefab, playerListParent);
                    TextMeshProUGUI itemText = item.GetComponentInChildren<TextMeshProUGUI>();
                    if (itemText != null)
                    {
                        string prefix = player.IsMasterClient ? "★ " : "  ";
                        string localTag = player.IsLocal ? " (You)" : "";
                        itemText.text = $"{prefix}{player.NickName}{localTag}";
                    }
                }
                else
                {
                    // Fallback: create a simple text object
                    GameObject item = new GameObject($"PlayerItem_{i}");
                    item.transform.SetParent(playerListParent);

                    TextMeshProUGUI text = item.AddComponent<TextMeshProUGUI>();
                    string prefix = player.IsMasterClient ? "★ " : "  ";
                    string localTag = player.IsLocal ? " (You)" : "";
                    text.text = $"{prefix}{player.NickName}{localTag}";
                    text.fontSize = 18;
                    text.color = Color.white;
                }
            }

            // Update room info
            if (roomInfoText != null && PhotonNetwork.InRoom)
            {
                roomInfoText.text = $"Room: {PhotonNetwork.CurrentRoom.Name}\n" +
                                   $"Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}";
            }
        }

        /// <summary>
        /// Sets the status text message.
        /// </summary>
        public void SetStatusText(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        /// <summary>
        /// Shows or hides the Start Game button (visible only to MasterClient).
        /// </summary>
        public void SetStartButtonVisible(bool visible)
        {
            if (startGameButton != null)
                startGameButton.gameObject.SetActive(visible);
        }

        #endregion

        #region Helpers

        private void UpdatePlayerName()
        {
            if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text))
            {
                PhotonNetwork.NickName = playerNameInput.text;
                PlayerPrefs.SetString("PlayerName", playerNameInput.text);
                PlayerPrefs.Save();
            }
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            if (playerCountDropdown != null)
                playerCountDropdown.onValueChanged.RemoveListener(OnPlayerCountChanged);

            if (boardSizeDropdown != null)
                boardSizeDropdown.onValueChanged.RemoveListener(OnBoardSizeChanged);

            if (createRoomButton != null)
                createRoomButton.onClick.RemoveListener(OnCreateRoomClicked);

            if (joinRoomButton != null)
                joinRoomButton.onClick.RemoveListener(OnJoinRoomClicked);

            if (joinRandomButton != null)
                joinRandomButton.onClick.RemoveListener(OnJoinRandomClicked);

            if (startGameButton != null)
                startGameButton.onClick.RemoveListener(OnStartGameClicked);

            if (disconnectButton != null)
                disconnectButton.onClick.RemoveListener(OnDisconnectClicked);
        }

        #endregion
    }
}
