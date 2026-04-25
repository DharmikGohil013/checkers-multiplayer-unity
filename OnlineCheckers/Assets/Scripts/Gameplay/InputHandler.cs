using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Checkers.Core;
using Checkers.Network;
using Checkers.Utilities;

namespace Checkers.Gameplay
{
    /// <summary>
    /// Handles player input via centralized raycasting (works on PC + Mobile).
    /// Implements a state machine: IDLE → PIECE_SELECTED → WAITING_FOR_NETWORK.
    /// Prevents input when it's not the local player's turn.
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        #region State Machine

        private enum InputState
        {
            IDLE,
            PIECE_SELECTED,
            WAITING_FOR_NETWORK
        }

        #endregion

        #region Fields

        private InputState _currentState = InputState.IDLE;
        private CheckersPiece _selectedPiece;
        private List<MoveData> _validMoves;
        private List<BoardCell> _highlightedCells;

        // Cached references
        private GameManager _gameManager;
        private BoardManager _boardManager;
        private TurnManager _turnManager;
        private NetworkGameManager _networkGameManager;
        private Camera _mainCamera;

        // Colors
        private readonly Color _moveHighlightColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);
        private readonly Color _captureHighlightColor = new Color(0.9f, 0.2f, 0.2f, 0.5f);

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _highlightedCells = new List<BoardCell>(16);
            _validMoves = new List<MoveData>(20);
        }

        private void Start()
        {
            _gameManager = GameManager.Instance;
            if (_gameManager != null)
            {
                _boardManager = _gameManager.BoardManager;
                _turnManager = _gameManager.TurnManager;
                _networkGameManager = _gameManager.NetworkGameManager;
            }

            _mainCamera = Camera.main;

            // Listen for turn changes to reset state
            if (_gameManager != null)
            {
                _gameManager.OnTurnChanged += HandleTurnChanged;
                _gameManager.OnGameEnd += HandleGameEnd;
            }
        }

        private void OnDestroy()
        {
            if (_gameManager != null)
            {
                _gameManager.OnTurnChanged -= HandleTurnChanged;
                _gameManager.OnGameEnd -= HandleGameEnd;
            }
        }

        /// <summary>
        /// Centralized input detection — replaces OnMouseDown on individual objects.
        /// Works on both PC (mouse) and Mobile (touch).
        /// </summary>
        private void Update()
        {
            // Detect click/tap
            bool inputDown = false;
            Vector3 screenPos = Vector3.zero;
            string inputType = "";

#if ENABLE_INPUT_SYSTEM
            // New Input System
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                inputDown = true;
                screenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                inputType = "Mouse (New System)";
            }
            else if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                inputDown = true;
                screenPos = UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
                inputType = "Touch (New System)";
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER || !ENABLE_INPUT_SYSTEM
            // Legacy Input Manager (Fallback)
            if (!inputDown)
            {
#if UNITY_EDITOR || UNITY_STANDALONE
                if (Input.GetMouseButtonDown(0))
                {
                    inputDown = true;
                    screenPos = Input.mousePosition;
                    inputType = "Mouse (Legacy)";
                }
#endif
                if (!inputDown && Input.touchCount > 0)
                {
                    Touch touch = Input.GetTouch(0);
                    if (touch.phase == TouchPhase.Began)
                    {
                        inputDown = true;
                        screenPos = touch.position;
                        inputType = "Touch (Legacy)";
                    }
                }
            }
#endif

            if (!inputDown)
                return;

            Debug.Log("=== INPUT PIPELINE START ===");
            Debug.Log("Touch → Raycast → Hit → TurnCheck → Select");

            Debug.Log($"[INPUT] Touch detected at screen: {screenPos}");

            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                // Basic check for UI blocking (works for mouse/touches depending on Unity version)
                if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    Debug.LogWarning("[UI BLOCK] Touch is over UI — blocking input");
                    // return; // Uncomment to actually block it, keeping just log for now
                }
            }

            if (!CanProcessInput())
            {
                Debug.Log($"[InputHandler] Input ignored: CanProcessInput() returned false. GameActive: {(_gameManager != null ? _gameManager.IsGameActive.ToString() : "null")}, LocalTurn: {(_turnManager != null ? _turnManager.IsLocalPlayerTurn().ToString() : "null")}");
                return;
            }

            // Ensure camera is available
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null)
                {
                    Debug.LogError("[InputHandler] Main Camera is null, cannot process input!");
                    return;
                }
            }

            // Convert screen position to world position
            Vector2 worldPos = _mainCamera.ScreenToWorldPoint(screenPos);
            Debug.Log($"[INPUT] Converted world position: {worldPos}");

            // Raycast to find what was clicked — check pieces FIRST (higher sorting order)
            RaycastHit2D[] hits = Physics2D.RaycastAll(worldPos, Vector2.zero);
            Debug.Log($"[RAYCAST] Hit count: {hits.Length}");

            if (hits.Length == 0)
            {
                Debug.LogWarning("[RAYCAST] NO OBJECT HIT — possible collider issue");
            }

            foreach (var hit in hits)
            {
                Debug.Log("[RAYCAST] Hit object: " + hit.collider.name + 
                          " | Layer: " + LayerMask.LayerToName(hit.collider.gameObject.layer));
            }

            CheckersPiece hitPiece = null;
            BoardCell hitCell = null;

            // Sort by priority: pieces first, then cells
            for (int i = 0; i < hits.Length; i++)
            {
                Debug.Log($"[InputHandler] Hit {i}: {hits[i].collider.gameObject.name}");
                
                if (hitPiece == null)
                {
                    CheckersPiece piece = hits[i].collider.GetComponent<CheckersPiece>();
                    if (piece != null)
                        hitPiece = piece;
                }

                if (hitCell == null)
                {
                    BoardCell cell = hits[i].collider.GetComponent<BoardCell>();
                    if (cell != null)
                        hitCell = cell;
                }
            }

            // Process the hit by calling the required methods on the objects themselves
            if (hitPiece != null)
            {
                Debug.Log($"[InputHandler] Selecting Piece: {hitPiece.gameObject.name} at ({hitPiece.Row},{hitPiece.Col})");
                hitPiece.OnPieceClicked();
            }
            else if (hitCell != null)
            {
                Debug.Log($"[InputHandler] Selecting Cell: {hitCell.gameObject.name} at ({hitCell.Row},{hitCell.Col})");
                hitCell.OnCellClicked();
            }
            else
            {
                Debug.Log($"[InputHandler] Raycast hit nothing playable. If clicking board, check colliders and Z-position.");
            }

            Debug.Log("=== INPUT PIPELINE END ===");
        }

        #endregion

        #region Event Handlers

        public void HandlePieceClicked(CheckersPiece piece)
        {
            if (!CanProcessInput())
                return;

            // Check if this piece belongs to the local player
            int localPlayerOwner = GetLocalPlayerOwnerIndex();
            if (piece.OwnerActorNumber != localPlayerOwner)
            {
                GameLogger.Log(GameLogger.LogLevel.INFO,
                    $"Clicked opponent's piece at ({piece.Row},{piece.Col}). Ignoring.");
                return;
            }

            switch (_currentState)
            {
                case InputState.IDLE:
                    SelectPiece(piece);
                    break;

                case InputState.PIECE_SELECTED:
                    // Clicking a different own piece — re-select
                    if (piece != _selectedPiece)
                    {
                        ClearSelection();
                        SelectPiece(piece);
                    }
                    else
                    {
                        // Clicking same piece — deselect
                        ClearSelection();
                    }
                    break;

                case InputState.WAITING_FOR_NETWORK:
                    // Ignore input while waiting
                    break;
            }
        }

        public void HandleCellClicked(BoardCell cell)
        {
            if (!CanProcessInput())
                return;

            switch (_currentState)
            {
                case InputState.IDLE:
                    // No piece selected, ignore cell clicks
                    break;

                case InputState.PIECE_SELECTED:
                    if (cell.IsHighlighted())
                    {
                        // Find the matching move
                        MoveData? matchedMove = FindMoveForCell(cell.Row, cell.Col);
                        if (matchedMove.HasValue)
                        {
                            ExecuteMove(matchedMove.Value);
                        }
                        else
                        {
                            GameLogger.Log(GameLogger.LogLevel.WARN,
                                $"Cell ({cell.Row},{cell.Col}) is highlighted but no matching move found.");
                        }
                    }
                    else
                    {
                        // Clicked non-highlighted cell — deselect
                        ClearSelection();
                    }
                    break;

                case InputState.WAITING_FOR_NETWORK:
                    break;
            }
        }

        private void HandleTurnChanged(int actorNumber)
        {
            // Reset input state when turn changes
            if (_currentState == InputState.WAITING_FOR_NETWORK)
            {
                _currentState = InputState.IDLE;
            }

            ClearSelection();

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Turn changed to Actor {actorNumber}. Input state reset.");
        }

        private void HandleGameEnd(int winnerActorNumber)
        {
            ClearSelection();
            _currentState = InputState.IDLE;
        }

        #endregion

        #region Piece Selection

        private void SelectPiece(CheckersPiece piece)
        {
            if (piece == null)
            {
                Debug.LogError("[SELECT] Piece is NULL");
                return;
            }

            Debug.Log("[SELECT] Trying to select: " + piece.name);

            _selectedPiece = piece;
            _selectedPiece.SetHighlight(true);

            // Get valid moves for this piece
            _validMoves.Clear();
            List<MoveData> moves = _boardManager.GetValidMoves(piece.Row, piece.Col);

            // Apply force capture filter across all player pieces
            if (_gameManager.GameSettings != null && _gameManager.GameSettings.forceCapture)
            {
                int localOwner = GetLocalPlayerOwnerIndex();
                List<MoveData> allMoves = _boardManager.GetAllValidMovesForPlayer(localOwner);

                bool globalCapture = false;
                for (int i = 0; i < allMoves.Count; i++)
                {
                    if (allMoves[i].isCapture)
                    {
                        globalCapture = true;
                        break;
                    }
                }

                if (globalCapture)
                {
                    // Only allow capture moves from this piece
                    for (int i = 0; i < moves.Count; i++)
                    {
                        if (moves[i].isCapture)
                            _validMoves.Add(moves[i]);
                    }
                }
                else
                {
                    _validMoves.AddRange(moves);
                }
            }
            else
            {
                _validMoves.AddRange(moves);
            }

            // Highlight valid destination cells
            HighlightValidMoves();

            if (_validMoves.Count > 0)
            {
                _currentState = InputState.PIECE_SELECTED;
                GameLogger.Log(GameLogger.LogLevel.INFO,
                    $"Piece selected at ({piece.Row},{piece.Col}). Valid moves: {_validMoves.Count}");
            }
            else
            {
                GameLogger.Log(GameLogger.LogLevel.INFO,
                    $"Piece at ({piece.Row},{piece.Col}) has no valid moves.");

                // Show invalid feedback
                if (UI.UIManager.Instance != null)
                    UI.UIManager.Instance.ShowInvalidMoveFlash();

                ClearSelection();
            }
        }

        private void ClearSelection()
        {
            if (_selectedPiece != null)
            {
                _selectedPiece.SetHighlight(false);
                _selectedPiece = null;
            }

            ClearHighlights();
            _validMoves.Clear();

            if (_currentState != InputState.WAITING_FOR_NETWORK)
                _currentState = InputState.IDLE;
        }

        #endregion

        #region Move Execution

        private void ExecuteMove(MoveData move)
        {
            _currentState = InputState.WAITING_FOR_NETWORK;

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Executing move: ({move.fromRow},{move.fromCol}) → ({move.toRow},{move.toCol}), " +
                $"Capture: {move.isCapture}");

            ClearHighlights();

            if (_selectedPiece != null)
                _selectedPiece.SetHighlight(false);

            // Send the move over the network
            if (_networkGameManager != null)
            {
                _networkGameManager.SendMove(move);
            }
            else
            {
                // Offline fallback (for testing)
                GameLogger.Log(GameLogger.LogLevel.WARN,
                    "NetworkGameManager not available. Executing move locally.");

                if (move.isCapture)
                    _boardManager.CapturePiece(move.captureRow, move.captureCol);

                _boardManager.MovePiece(move.fromRow, move.fromCol, move.toRow, move.toCol);

                // Check promotion
                PieceData movedPiece = _boardManager.BoardState[move.toRow, move.toCol];
                if (!movedPiece.isKing && _gameManager.GameSettings != null && _gameManager.GameSettings.kingPromotion)
                {
                    if (CheckersRules.CanPromoteToKing(move.toRow, movedPiece.playerOwner, _boardManager.BoardSize))
                    {
                        _boardManager.PromoteToKing(move.toRow, move.toCol);
                    }
                }

                _turnManager.NextTurn();
            }

            _selectedPiece = null;
        }

        #endregion

        #region Highlighting

        private void HighlightValidMoves()
        {
            ClearHighlights();

            for (int i = 0; i < _validMoves.Count; i++)
            {
                MoveData move = _validMoves[i];
                BoardCell cell = _boardManager.GetCellAt(move.toRow, move.toCol);

                if (cell != null)
                {
                    Color highlightColor = move.isCapture ? _captureHighlightColor : _moveHighlightColor;
                    cell.SetHighlight(true, highlightColor);
                    _highlightedCells.Add(cell);
                }
            }
        }

        private void ClearHighlights()
        {
            for (int i = 0; i < _highlightedCells.Count; i++)
            {
                if (_highlightedCells[i] != null)
                    _highlightedCells[i].SetHighlight(false);
            }
            _highlightedCells.Clear();
        }

        #endregion

        #region Helpers

        private bool CanProcessInput()
        {
            if (_gameManager == null || !_gameManager.IsGameActive)
            {
                Debug.Log($"[InputHandler] CanProcessInput: false (GameActive is false or GameManager is null)");
                return false;
            }

            bool isLocalTurn = _turnManager != null && _turnManager.IsLocalPlayerTurn();
            int currentTurn = _turnManager != null ? _turnManager.GetCurrentPlayer() : -1;
            int myActor = PhotonNetwork.InRoom ? PhotonNetwork.LocalPlayer.ActorNumber : -1;

            Debug.Log($"[TURN CHECK] LocalPlayer: {myActor} | CurrentTurn: {currentTurn}");

            if (!isLocalTurn)
            {
                Debug.LogWarning("[TURN BLOCK] Input blocked — NOT your turn");
            }
            
            return isLocalTurn;
        }

        private MoveData? FindMoveForCell(int targetRow, int targetCol)
        {
            for (int i = 0; i < _validMoves.Count; i++)
            {
                if (_validMoves[i].toRow == targetRow && _validMoves[i].toCol == targetCol)
                    return _validMoves[i];
            }
            return null;
        }

        /// <summary>
        /// Maps the local Photon actor number to a player owner index (1-based).
        /// Based on the turn order array.
        /// </summary>
        private int GetLocalPlayerOwnerIndex()
        {
            if (_turnManager == null || !PhotonNetwork.IsConnected)
                return 1;

            int[] actorNumbers = _turnManager.GetPlayerActorNumbers();
            if (actorNumbers == null)
                return 1;

            int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
            for (int i = 0; i < actorNumbers.Length; i++)
            {
                if (actorNumbers[i] == localActor)
                    return i + 1; // 1-based player owner
            }

            return 1;
        }

        #endregion
    }
}
