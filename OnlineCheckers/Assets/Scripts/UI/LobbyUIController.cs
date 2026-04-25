using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using Checkers.Network;
using Checkers.Utilities;
namespace Checkers.UI
{
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
            if (networkLobbyManager == null)
                networkLobbyManager = FindAnyObjectByType<NetworkLobbyManager>();
            SetupDropdowns();
            SetupButtons();
            SetStartButtonVisible(false);
            string savedName = PlayerPrefs.GetString("PlayerName", "Player" + Random.Range(100, 999));
            PhotonNetwork.NickName = savedName;
            if (playerNameInput != null)
                playerNameInput.text = savedName;
        }
        #endregion
        #region Setup
        private void SetupDropdowns()
        {
            if (playerCountDropdown != null)
            {
                playerCountDropdown.ClearOptions();
                playerCountDropdown.AddOptions(new System.Collections.Generic.List<string> { "2 Players", "4 Players" });
                playerCountDropdown.value = 0;
                playerCountDropdown.onValueChanged.AddListener(OnPlayerCountChanged);
            }
            if (boardSizeDropdown != null)
            {
                boardSizeDropdown.ClearOptions();
                boardSizeDropdown.AddOptions(new System.Collections.Generic.List<string> { "4x4", "6x6", "8x8" });
                boardSizeDropdown.value = 2;
                boardSizeDropdown.onValueChanged.AddListener(OnBoardSizeChanged);
            }
        }
        private void SetupButtons()
        {
            createRoomButton?.onClick.AddListener(OnCreateRoomClicked);
            joinRoomButton?.onClick.AddListener(OnJoinRoomClicked);
            joinRandomButton?.onClick.AddListener(OnJoinRandomClicked);
            startGameButton?.onClick.AddListener(OnStartGameClicked);
            disconnectButton?.onClick.AddListener(OnDisconnectClicked);
        }
        #endregion
        #region Button Handlers
        private void OnCreateRoomClicked()
        {
            UpdatePlayerName();
            string roomName = roomNameInput != null ? roomNameInput.text.Trim() : "";
            networkLobbyManager?.CreateRoom(roomName);
        }
        private void OnJoinRoomClicked()
        {
            UpdatePlayerName();
            string roomName = roomNameInput != null ? roomNameInput.text.Trim() : "";
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
            SetStartButtonVisible(false);
            SetStatusText("Disconnected.");
        }
        private void OnPlayerCountChanged(int index)
        {
            int count = index == 0 ? 2 : 4;
            networkLobbyManager?.SetPlayerCount(count);
        }
        private void OnBoardSizeChanged(int index)
        {
            int[] sizes = { 4, 6, 8 };
            int size = sizes[Mathf.Clamp(index, 0, sizes.Length - 1)];
            networkLobbyManager?.SetBoardSize(size);
        }
        #endregion
        #region Public API (called by NetworkLobbyManager callbacks)
        public void OnJoinedRoom()
        {
            if (joinPanel != null)
                joinPanel.SetActive(false);
            if (roomInfoPanel != null)
                roomInfoPanel.SetActive(true);
            UpdateRoomInfoText();
            RefreshStartButton();
        }
        public void UpdatePlayerList(Player[] players)
        {
            if (playerListParent == null) return;
            for (int i = playerListParent.childCount - 1; i >= 0; i--)
            {
                Destroy(playerListParent.GetChild(i).gameObject);
            }
            foreach (Player player in players)
            {
                string prefix = player.IsMasterClient ? "[Host] " : "";
                string localTag = player.IsLocal ? " (You)" : "";
                string displayName = string.IsNullOrEmpty(player.NickName)
                    ? $"Player {player.ActorNumber}"
                    : player.NickName;
                string fullText = $"{prefix}{displayName}{localTag}";
                if (playerListItemPrefab != null)
                {
                    GameObject item = Instantiate(playerListItemPrefab, playerListParent);
                    TextMeshProUGUI tmp = item.GetComponent<TextMeshProUGUI>();
                    if (tmp == null)
                        tmp = item.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null)
                        tmp.text = fullText;
                }
                else
                {
                    GameObject item = new GameObject($"Player_{player.ActorNumber}");
                    RectTransform rt = item.AddComponent<RectTransform>();
                    rt.SetParent(playerListParent, false);
                    rt.sizeDelta = new Vector2(280f, 40f);
                    TextMeshProUGUI tmp = item.AddComponent<TextMeshProUGUI>();
                    tmp.text = fullText;
                    tmp.fontSize = 20;
                    tmp.color = Color.white;
                    tmp.alignment = TextAlignmentOptions.MidlineLeft;
                }
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(playerListParent as RectTransform);
            UpdateRoomInfoText();
            RefreshStartButton();
        }
        public void SetStatusText(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }
        public void SetStartButtonVisible(bool visible)
        {
            if (startGameButton != null)
                startGameButton.gameObject.SetActive(visible);
        }
        #endregion
        #region Start Button Logic
        private void RefreshStartButton()
        {
            if (!PhotonNetwork.InRoom)
            {
                SetStartButtonVisible(false);
                return;
            }
            bool isMaster = PhotonNetwork.IsMasterClient;
            int count = PhotonNetwork.CurrentRoom.PlayerCount;
            Debug.Log($"[LobbyUI] RefreshStartButton — IsMaster:{isMaster} PlayerCount:{count}");
            SetStartButtonVisible(isMaster && count >= 2);
        }
        #endregion
        #region Helpers
        private void UpdateRoomInfoText()
        {
            if (roomInfoText != null && PhotonNetwork.InRoom)
            {
                roomInfoText.text =
                    $"Room: {PhotonNetwork.CurrentRoom.Name}  " +
                    $"Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}";
            }
        }
        private void UpdatePlayerName()
        {
            if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text))
            {
                PhotonNetwork.NickName = playerNameInput.text.Trim();
                PlayerPrefs.SetString("PlayerName", PhotonNetwork.NickName);
                PlayerPrefs.Save();
            }
        }
        #endregion
        #region Cleanup
        private void OnDestroy()
        {
            playerCountDropdown?.onValueChanged.RemoveListener(OnPlayerCountChanged);
            boardSizeDropdown?.onValueChanged.RemoveListener(OnBoardSizeChanged);
            createRoomButton?.onClick.RemoveListener(OnCreateRoomClicked);
            joinRoomButton?.onClick.RemoveListener(OnJoinRoomClicked);
            joinRandomButton?.onClick.RemoveListener(OnJoinRandomClicked);
            startGameButton?.onClick.RemoveListener(OnStartGameClicked);
            disconnectButton?.onClick.RemoveListener(OnDisconnectClicked);
        }
        #endregion
    }
}