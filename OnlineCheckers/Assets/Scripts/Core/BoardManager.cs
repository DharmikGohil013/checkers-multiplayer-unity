using System;
using System.Collections.Generic;
using UnityEngine;
using Checkers.Data;
using Checkers.Gameplay;
using Checkers.Network;
using Checkers.Utilities;

namespace Checkers.Core
{
    /// <summary>
    /// Struct representing a single cell on the board.
    /// </summary>
    [Serializable]
    public struct PieceData
    {
        /// <summary>Player owner index (1-based). 0 = empty cell.</summary>
        public int playerOwner;

        /// <summary>Whether this piece has been promoted to king.</summary>
        public bool isKing;

        /// <summary>Returns true if this cell has no piece.</summary>
        public bool IsEmpty => playerOwner == 0;

        public PieceData(int owner, bool king)
        {
            playerOwner = owner;
            isKing = king;
        }

        public static PieceData Empty => new PieceData(0, false);
    }

    /// <summary>
    /// Struct representing a single move on the board.
    /// </summary>
    [Serializable]
    public struct MoveData
    {
        public int fromRow;
        public int fromCol;
        public int toRow;
        public int toCol;
        public bool isCapture;
        public int captureRow;
        public int captureCol;

        public MoveData(int fRow, int fCol, int tRow, int tCol, bool capture = false, int capRow = -1, int capCol = -1)
        {
            fromRow = fRow;
            fromCol = fCol;
            toRow = tRow;
            toCol = tCol;
            isCapture = capture;
            captureRow = capRow;
            captureCol = capCol;
        }
    }

    /// <summary>
    /// Manages the board state, piece placement, movement, captures, and visual representation.
    /// Uses object pooling for piece GameObjects. All logic is index-based (row, col).
    /// </summary>
    public class BoardManager : MonoBehaviour
    {
        #region Fields

        [Header("Prefabs")]
        [SerializeField] private GameObject piecePrefab;
        [SerializeField] private GameObject cellPrefab;

        [Header("Board Parent")]
        [SerializeField] private Transform boardParent;

        // Board state — deterministic, index-based
        private PieceData[,] _boardState;
        private int _boardSize;
        private int _playerCount;

        // Visual representation
        private BoardCell[,] _cells;
        private CheckersPiece[,] _pieceObjects;

        // Object pools
        private ObjectPool<CheckersPiece> _piecePool;
        private ObjectPool<BoardCell> _cellPool;

        // Pre-allocated lists to avoid GC
        private readonly List<MoveData> _moveCache = new List<MoveData>(20);
        private readonly List<MoveData> _allMovesCache = new List<MoveData>(64);

        // Cached configs
        private BoardConfig _boardConfig;
        private PieceConfig _pieceConfig;
        private GameSettings _gameSettings;

        public PieceData[,] BoardState => _boardState;
        public int BoardSize => _boardSize;
        public int PlayerCount => _playerCount;

        #endregion

        #region Events

        /// <summary>Fired when a piece is moved visually.</summary>
        public event Action<int, int, int, int> OnPieceMoved; // fromRow, fromCol, toRow, toCol

        /// <summary>Fired when a piece is captured.</summary>
        public event Action<int, int> OnPieceCaptured; // row, col

        /// <summary>Fired when a piece is promoted to king.</summary>
        public event Action<int, int> OnPiecePromoted; // row, col

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (boardParent == null)
                boardParent = transform;

            // Initialize pools
            _piecePool = gameObject.AddComponent<ObjectPool<CheckersPiece>>();
            _cellPool = gameObject.AddComponent<ObjectPool<BoardCell>>();
        }

        private void Start()
        {
            _boardConfig = GameManager.Instance.BoardConfig;
            _pieceConfig = GameManager.Instance.PieceConfig;
            _gameSettings = GameManager.Instance.GameSettings;

            if (piecePrefab != null)
                _piecePool.Initialize(piecePrefab.GetComponent<CheckersPiece>(), Constants.POOL_PIECE_SIZE);

            if (cellPrefab != null)
                _cellPool.Initialize(cellPrefab.GetComponent<BoardCell>(), Constants.POOL_HIGHLIGHT_SIZE);
        }

        #endregion

        #region Board Initialization

        /// <summary>
        /// Initializes the board with the specified size and player count.
        /// Creates the visual cells and places initial pieces.
        /// </summary>
        public void InitializeBoard(int size, int playerCount)
        {
            _boardSize = size;
            _playerCount = playerCount;
            _boardState = new PieceData[size, size];
            _cells = new BoardCell[size, size];
            _pieceObjects = new CheckersPiece[size, size];

            // Clear existing board visuals
            ClearBoard();

            // Create visual cells
            CreateVisualBoard();

            // Place initial pieces
            PlaceInitialPieces();

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Board initialized: {size}x{size}, {playerCount} players, {_boardConfig.GetPiecesPerPlayer()} pieces each.");
        }

        private void ClearBoard()
        {
            if (_cells != null)
            {
                for (int r = 0; r < _cells.GetLength(0); r++)
                {
                    for (int c = 0; c < _cells.GetLength(1); c++)
                    {
                        if (_cells[r, c] != null)
                        {
                            _cellPool.Return(_cells[r, c]);
                            _cells[r, c] = null;
                        }
                    }
                }
            }

            if (_pieceObjects != null)
            {
                for (int r = 0; r < _pieceObjects.GetLength(0); r++)
                {
                    for (int c = 0; c < _pieceObjects.GetLength(1); c++)
                    {
                        if (_pieceObjects[r, c] != null)
                        {
                            _piecePool.Return(_pieceObjects[r, c]);
                            _pieceObjects[r, c] = null;
                        }
                    }
                }
            }
        }

        private void CreateVisualBoard()
        {
            float offset = (_boardSize * _boardConfig.cellSize) / 2f - _boardConfig.cellSize / 2f;

            for (int row = 0; row < _boardSize; row++)
            {
                for (int col = 0; col < _boardSize; col++)
                {
                    bool isDark = (row + col) % 2 == 1;
                    Color cellColor = isDark ? _boardConfig.darkCellColor : _boardConfig.lightCellColor;

                    BoardCell cell;

                    if (cellPrefab != null)
                    {
                        cell = _cellPool.Get();
                        cell.transform.SetParent(boardParent);
                    }
                    else
                    {
                        // Create a simple quad if no prefab assigned
                        GameObject cellGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        cellGO.transform.SetParent(boardParent);
                        cell = cellGO.AddComponent<BoardCell>();
                    }

                    Vector3 worldPos = new Vector3(
                        col * _boardConfig.cellSize - offset,
                        row * _boardConfig.cellSize - offset,
                        0f
                    );

                    cell.transform.localPosition = worldPos;
                    cell.transform.localScale = Vector3.one * _boardConfig.cellSize;
                    cell.Initialize(row, col, cellColor);
                    cell.gameObject.name = $"Cell_{row}_{col}";

                    _cells[row, col] = cell;
                }
            }
        }

        private void PlaceInitialPieces()
        {
            if (_playerCount == 2)
            {
                PlacePiecesForTwoPlayers();
            }
            else if (_playerCount == 4)
            {
                PlacePiecesForFourPlayers();
            }
        }

        private void PlacePiecesForTwoPlayers()
        {
            int rowsOfPieces = (_boardSize / 2) - 1;

            // Player 1 — bottom rows
            for (int row = 0; row < rowsOfPieces; row++)
            {
                for (int col = 0; col < _boardSize; col++)
                {
                    if ((row + col) % 2 == 1)
                    {
                        PlacePiece(row, col, 1);
                    }
                }
            }

            // Player 2 — top rows
            for (int row = _boardSize - rowsOfPieces; row < _boardSize; row++)
            {
                for (int col = 0; col < _boardSize; col++)
                {
                    if ((row + col) % 2 == 1)
                    {
                        PlacePiece(row, col, 2);
                    }
                }
            }
        }

        private void PlacePiecesForFourPlayers()
        {
            int quarterRows = Mathf.Max(1, (_boardSize / 4));
            int halfSize = _boardSize / 2;

            // Player 1 — bottom-left quadrant
            for (int row = 0; row < quarterRows; row++)
            {
                for (int col = 0; col < halfSize; col++)
                {
                    if ((row + col) % 2 == 1)
                        PlacePiece(row, col, 1);
                }
            }

            // Player 2 — top-right quadrant
            for (int row = _boardSize - quarterRows; row < _boardSize; row++)
            {
                for (int col = halfSize; col < _boardSize; col++)
                {
                    if ((row + col) % 2 == 1)
                        PlacePiece(row, col, 2);
                }
            }

            // Player 3 — top-left quadrant
            for (int row = _boardSize - quarterRows; row < _boardSize; row++)
            {
                for (int col = 0; col < halfSize; col++)
                {
                    if ((row + col) % 2 == 1)
                        PlacePiece(row, col, 3);
                }
            }

            // Player 4 — bottom-right quadrant
            for (int row = 0; row < quarterRows; row++)
            {
                for (int col = halfSize; col < _boardSize; col++)
                {
                    if ((row + col) % 2 == 1)
                        PlacePiece(row, col, 4);
                }
            }
        }

        #endregion

        #region Board Operations

        /// <summary>
        /// Places a piece on the board at the specified position.
        /// </summary>
        public void PlacePiece(int row, int col, int playerOwner)
        {
            if (!IsInBounds(row, col))
            {
                GameLogger.Log(GameLogger.LogLevel.ERROR, $"PlacePiece out of bounds: ({row},{col})");
                return;
            }

            _boardState[row, col] = new PieceData(playerOwner, false);

            // Create visual piece
            CreateVisualPiece(row, col, playerOwner, false);

            GameLogger.Log(GameLogger.LogLevel.INFO, $"Placed piece for Player {playerOwner} at ({row},{col})");
        }

        /// <summary>
        /// Moves a piece from one position to another. Does NOT validate the move.
        /// </summary>
        public void MovePiece(int fromRow, int fromCol, int toRow, int toCol)
        {
            if (!IsInBounds(fromRow, fromCol) || !IsInBounds(toRow, toCol))
            {
                GameLogger.Log(GameLogger.LogLevel.ERROR,
                    $"MovePiece out of bounds: ({fromRow},{fromCol}) → ({toRow},{toCol})");
                return;
            }

            PieceData piece = _boardState[fromRow, fromCol];
            if (piece.IsEmpty)
            {
                GameLogger.Log(GameLogger.LogLevel.WARN,
                    $"MovePiece called on empty cell ({fromRow},{fromCol})");
                return;
            }

            // Update logical state
            _boardState[toRow, toCol] = piece;
            _boardState[fromRow, fromCol] = PieceData.Empty;

            // Update visual
            CheckersPiece visualPiece = _pieceObjects[fromRow, fromCol];
            if (visualPiece != null)
            {
                visualPiece.MoveTo(toRow, toCol);
                _pieceObjects[toRow, toCol] = visualPiece;
                _pieceObjects[fromRow, fromCol] = null;
            }

            OnPieceMoved?.Invoke(fromRow, fromCol, toRow, toCol);

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Moved piece ({fromRow},{fromCol}) → ({toRow},{toCol})");
        }

        /// <summary>
        /// Captures (removes) the piece at the specified position.
        /// </summary>
        public void CapturePiece(int row, int col)
        {
            if (!IsInBounds(row, col))
            {
                GameLogger.Log(GameLogger.LogLevel.ERROR, $"CapturePiece out of bounds: ({row},{col})");
                return;
            }

            if (_boardState[row, col].IsEmpty)
            {
                GameLogger.Log(GameLogger.LogLevel.WARN, $"CapturePiece called on empty cell ({row},{col})");
                return;
            }

            int capturedOwner = _boardState[row, col].playerOwner;
            _boardState[row, col] = PieceData.Empty;

            // Return visual piece to pool
            CheckersPiece visualPiece = _pieceObjects[row, col];
            if (visualPiece != null)
            {
                visualPiece.ReturnToPool();
                _piecePool.Return(visualPiece);
                _pieceObjects[row, col] = null;
            }

            OnPieceCaptured?.Invoke(row, col);

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Captured Player {capturedOwner}'s piece at ({row},{col})");
        }

        /// <summary>
        /// Promotes the piece at the specified position to king.
        /// </summary>
        public void PromoteToKing(int row, int col)
        {
            if (!IsInBounds(row, col) || _boardState[row, col].IsEmpty)
            {
                GameLogger.Log(GameLogger.LogLevel.WARN, $"PromoteToKing invalid at ({row},{col})");
                return;
            }

            PieceData piece = _boardState[row, col];
            piece.isKing = true;
            _boardState[row, col] = piece;

            CheckersPiece visualPiece = _pieceObjects[row, col];
            if (visualPiece != null)
            {
                visualPiece.PromoteToKing();
            }

            OnPiecePromoted?.Invoke(row, col);

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Promoted piece at ({row},{col}) to King");
        }

        #endregion

        #region Move Queries

        /// <summary>
        /// Gets all valid moves for a piece at the specified position.
        /// Uses pre-allocated list to avoid GC.
        /// </summary>
        public List<MoveData> GetValidMoves(int row, int col)
        {
            _moveCache.Clear();

            if (!IsInBounds(row, col) || _boardState[row, col].IsEmpty)
                return _moveCache;

            List<MoveData> moves = CheckersRules.GetValidMovesForPiece(
                _boardState, row, col, _gameSettings);

            _moveCache.AddRange(moves);
            return _moveCache;
        }

        /// <summary>
        /// Gets all valid moves for a specified player.
        /// Uses pre-allocated list to avoid GC.
        /// </summary>
        public List<MoveData> GetAllValidMovesForPlayer(int playerOwner)
        {
            _allMovesCache.Clear();

            for (int r = 0; r < _boardSize; r++)
            {
                for (int c = 0; c < _boardSize; c++)
                {
                    if (_boardState[r, c].playerOwner == playerOwner)
                    {
                        List<MoveData> pieceMoves = CheckersRules.GetValidMovesForPiece(
                            _boardState, r, c, _gameSettings);
                        _allMovesCache.AddRange(pieceMoves);
                    }
                }
            }

            // If force capture is on, filter to only capture moves if any exist
            if (_gameSettings.forceCapture)
            {
                bool hasCapture = false;
                for (int i = 0; i < _allMovesCache.Count; i++)
                {
                    if (_allMovesCache[i].isCapture)
                    {
                        hasCapture = true;
                        break;
                    }
                }

                if (hasCapture)
                {
                    for (int i = _allMovesCache.Count - 1; i >= 0; i--)
                    {
                        if (!_allMovesCache[i].isCapture)
                            _allMovesCache.RemoveAt(i);
                    }
                }
            }

            return _allMovesCache;
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Serializes the current board state to a JSON string.
        /// </summary>
        public string SerializeBoardState()
        {
            int turnIndex = GameManager.Instance.TurnManager != null
                ? GameManager.Instance.TurnManager.CurrentTurnIndex
                : 0;

            return StateSerializer.SerializeBoard(_boardState, turnIndex);
        }

        /// <summary>
        /// Restores the board state from a serialized JSON string.
        /// </summary>
        public void DeserializeBoardState(string json)
        {
            int turnIndex;
            PieceData[,] restored = StateSerializer.DeserializeBoard(json, out turnIndex);

            if (restored == null)
            {
                GameLogger.Log(GameLogger.LogLevel.ERROR, "Failed to deserialize board state.");
                return;
            }

            int size = restored.GetLength(0);
            _boardSize = size;
            _boardState = restored;

            // Rebuild visuals
            ClearBoard();

            if (_cells == null || _cells.GetLength(0) != size)
            {
                _cells = new BoardCell[size, size];
                _pieceObjects = new CheckersPiece[size, size];
                CreateVisualBoard();
            }

            // Re-create piece visuals from state
            if (_pieceObjects == null || _pieceObjects.GetLength(0) != size)
                _pieceObjects = new CheckersPiece[size, size];

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    if (!_boardState[r, c].IsEmpty)
                    {
                        CreateVisualPiece(r, c, _boardState[r, c].playerOwner, _boardState[r, c].isKing);
                    }
                }
            }

            GameLogger.Log(GameLogger.LogLevel.INFO, $"Board state deserialized. Size: {size}x{size}");
        }

        #endregion

        #region Piece Counting

        /// <summary>
        /// Counts the number of pieces a player has on the board.
        /// </summary>
        public int GetPieceCount(int playerOwner)
        {
            int count = 0;
            for (int r = 0; r < _boardSize; r++)
            {
                for (int c = 0; c < _boardSize; c++)
                {
                    if (_boardState[r, c].playerOwner == playerOwner)
                        count++;
                }
            }
            return count;
        }

        #endregion

        #region Helpers

        private bool IsInBounds(int row, int col)
        {
            return row >= 0 && row < _boardSize && col >= 0 && col < _boardSize;
        }

        private void CreateVisualPiece(int row, int col, int playerOwner, bool isKing)
        {
            if (_pieceConfig == null)
                return;

            CheckersPiece piece = null;

            if (piecePrefab != null && _piecePool != null)
            {
                piece = _piecePool.Get();
            }
            else
            {
                // Fallback: create a simple sprite object
                GameObject go = new GameObject($"Piece_{row}_{col}");
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 1;
                piece = go.AddComponent<CheckersPiece>();
            }

            if (piece == null)
                return;

            float offset = (_boardSize * _boardConfig.cellSize) / 2f - _boardConfig.cellSize / 2f;
            Vector3 worldPos = new Vector3(
                col * _boardConfig.cellSize - offset,
                row * _boardConfig.cellSize - offset,
                -0.1f
            );

            piece.transform.SetParent(boardParent);
            piece.transform.localPosition = worldPos;
            piece.Initialize(playerOwner, row, col, _pieceConfig);

            if (isKing)
                piece.PromoteToKing();

            piece.gameObject.SetActive(true);
            piece.gameObject.name = $"Piece_P{playerOwner}_{row}_{col}";

            _pieceObjects[row, col] = piece;
        }

        /// <summary>
        /// Gets the visual piece at the specified board position.
        /// </summary>
        public CheckersPiece GetPieceAt(int row, int col)
        {
            if (!IsInBounds(row, col))
                return null;
            return _pieceObjects[row, col];
        }

        /// <summary>
        /// Gets the cell at the specified board position.
        /// </summary>
        public BoardCell GetCellAt(int row, int col)
        {
            if (!IsInBounds(row, col))
                return null;
            return _cells[row, col];
        }

        /// <summary>
        /// Gets world position for a given board cell.
        /// </summary>
        public Vector3 GetWorldPosition(int row, int col)
        {
            float offset = (_boardSize * _boardConfig.cellSize) / 2f - _boardConfig.cellSize / 2f;
            return new Vector3(
                col * _boardConfig.cellSize - offset,
                row * _boardConfig.cellSize - offset,
                0f
            );
        }

        #endregion
    }
}
