// Assets/Scripts/Core/BoardManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Checkers.Data;
using Checkers.Gameplay;
using Checkers.Network;
using Checkers.Utilities;

namespace Checkers.Core
{
    [Serializable]
    public struct PieceData
    {
        public int  playerOwner;
        public bool isKing;
        public bool IsEmpty => playerOwner == 0;

        public PieceData(int owner, bool king) { playerOwner = owner; isKing = king; }
        public static PieceData Empty => new PieceData(0, false);
    }

    [Serializable]
    public struct MoveData
    {
        public int  fromRow, fromCol, toRow, toCol;
        public bool isCapture;
        public int  captureRow, captureCol;

        public MoveData(int fRow, int fCol, int tRow, int tCol,
                        bool capture = false, int capRow = -1, int capCol = -1)
        {
            fromRow = fRow; fromCol = fCol;
            toRow   = tRow; toCol   = tCol;
            isCapture   = capture;
            captureRow  = capRow;
            captureCol  = capCol;
        }
    }

    public class BoardManager : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Prefabs")]
        [SerializeField] private GameObject piecePrefab;
        [SerializeField] private GameObject cellPrefab;

        [Header("Board Parent")]
        [SerializeField] private Transform boardParent;

        #endregion

        #region Private State

        private PieceData[,]   _boardState;
        private BoardCell[,]   _cells;
        private CheckersPiece[,] _pieceObjects;

        private int _boardSize;
        private int _playerCount;

        private PiecePool _piecePool;
        private CellPool  _cellPool;

        // Pre-allocated to avoid per-frame GC
        private readonly List<MoveData> _moveCache     = new List<MoveData>(20);
        private readonly List<MoveData> _allMovesCache = new List<MoveData>(64);

        // Configs — loaded in EnsureConfigs(), NOT in Start()
        private BoardConfig  _boardConfig;
        private PieceConfig  _pieceConfig;
        private GameSettings _gameSettings;

        #endregion

        #region Public Accessors

        public PieceData[,] BoardState  => _boardState;
        public int          BoardSize   => _boardSize;
        public int          PlayerCount => _playerCount;

        #endregion

        #region Events

        public event Action<int,int,int,int> OnPieceMoved;    // fromRow,fromCol,toRow,toCol
        public event Action<int,int>         OnPieceCaptured; // row,col
        public event Action<int,int>         OnPiecePromoted; // row,col

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (boardParent == null) boardParent = transform;

            // Get or create pool components — they must exist before InitializeBoard is called
            _piecePool = GetComponent<PiecePool>();
            _cellPool  = GetComponent<CellPool>();

            if (_piecePool == null)
            {
                Debug.LogWarning("[BoardManager] PiecePool not found on GameManagers — adding it now. " +
                                 "Assign the Prefab field manually in the Inspector.");
                _piecePool = gameObject.AddComponent<PiecePool>();
            }

            if (_cellPool == null)
            {
                Debug.LogWarning("[BoardManager] CellPool not found on GameManagers — adding it now. " +
                                 "Assign the Prefab field manually in the Inspector.");
                _cellPool = gameObject.AddComponent<CellPool>();
            }
        }

        private void Start()
        {
            // Setup pools using Inspector-assigned prefabs (safe fallback)
            if (piecePrefab != null)
                _piecePool.Setup(piecePrefab, boardParent, 24);

            if (cellPrefab != null)
                _cellPool.Setup(cellPrefab, boardParent, 64);
        }

        #endregion

        #region Config Loading

        /// <summary>
        /// Called before any board operation that needs configs.
        /// Pulls from GameManager.Instance if not already loaded.
        /// Falls back to defaults if GameManager is unavailable.
        /// </summary>
        private void EnsureConfigs()
        {
            if (_boardConfig != null && _pieceConfig != null && _gameSettings != null)
                return;

            if (GameManager.Instance != null)
            {
                _boardConfig  = GameManager.Instance.BoardConfig;
                _pieceConfig  = GameManager.Instance.PieceConfig;
                _gameSettings = GameManager.Instance.GameSettings;
            }

            // Hard fallback — create default instances so nothing NPEs
            if (_boardConfig == null)
            {
                _boardConfig = ScriptableObject.CreateInstance<BoardConfig>();
                Debug.LogWarning("[BoardManager] BoardConfig not found — using defaults.");
            }
            if (_pieceConfig == null)
            {
                _pieceConfig = ScriptableObject.CreateInstance<PieceConfig>();
                Debug.LogWarning("[BoardManager] PieceConfig not found — using defaults.");
            }
            if (_gameSettings == null)
            {
                _gameSettings = ScriptableObject.CreateInstance<GameSettings>();
                Debug.LogWarning("[BoardManager] GameSettings not found — using defaults.");
            }
        }

        #endregion

        #region Board Initialization

        public void InitializeBoard(int size, int playerCount)
        {
            // Pull configs FIRST — before any visual creation
            EnsureConfigs();

            // Also setup pools here in case Start() hasn't run yet
            // (Start runs after all Awake calls, but InitializeBoard can be called
            //  from NetworkGameManager RPC which arrives right after scene load)
            if (piecePrefab != null && _piecePool.PooledCount == 0 && _piecePool.ActiveCount == 0)
                _piecePool.Setup(piecePrefab, boardParent, 24);

            if (cellPrefab != null && _cellPool.PooledCount == 0 && _cellPool.ActiveCount == 0)
                _cellPool.Setup(cellPrefab, boardParent, 64);

            // IMPORTANT: Reset boardParent transform to identity so all children
            // are placed in clean local space. This prevents scale/rotation distortion.
            if (boardParent != null)
            {
                boardParent.localPosition = Vector3.zero;
                boardParent.localRotation = Quaternion.identity;
                boardParent.localScale = Vector3.one;
            }

            _boardSize   = size;
            _playerCount = playerCount;
            _boardState  = new PieceData[size, size];
            _cells        = new BoardCell[size, size];
            _pieceObjects = new CheckersPiece[size, size];

            ClearBoard();
            CreateVisualBoard();
            PlaceInitialPieces();

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Board initialized: {size}x{size}, {playerCount} players.");
        }

        private void ClearBoard()
        {
            if (_cells != null)
            {
                int rows = _cells.GetLength(0);
                int cols = _cells.GetLength(1);
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                        if (_cells[r, c] != null) { _cellPool.ReturnCell(_cells[r, c]); _cells[r, c] = null; }
            }

            if (_pieceObjects != null)
            {
                int rows = _pieceObjects.GetLength(0);
                int cols = _pieceObjects.GetLength(1);
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                        if (_pieceObjects[r, c] != null) { _piecePool.ReturnPiece(_pieceObjects[r, c]); _pieceObjects[r, c] = null; }
            }
        }

        private void CreateVisualBoard()
        {
            // _boardConfig MUST be valid before this runs — guaranteed by EnsureConfigs()
            float cellSize = _boardConfig.cellSize > 0 ? _boardConfig.cellSize : 1f;
            float offset   = (_boardSize * cellSize) / 2f - cellSize / 2f;

            for (int row = 0; row < _boardSize; row++)
            {
                for (int col = 0; col < _boardSize; col++)
                {
                    bool  isDark    = (row + col) % 2 == 1;
                    Color cellColor = isDark ? _boardConfig.darkCellColor : _boardConfig.lightCellColor;

                    BoardCell cell;

                    if (cellPrefab != null)
                    {
                        cell = _cellPool.GetCell();
                        if (cell == null)
                        {
                            Debug.LogError("[BoardManager] CellPool returned null. Check CellPool Prefab field.");
                            continue;
                        }
                        cell.transform.SetParent(boardParent);
                    }
                    else
                    {
                        // Fallback quad — works without a prefab
                        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        go.transform.SetParent(boardParent);
                        cell = go.GetComponent<BoardCell>();
                        if (cell == null) cell = go.AddComponent<BoardCell>();
                    }

                    // CRITICAL: Reset transform completely before setting new values.
                    // Pool returns objects with stale scale/rotation from previous use.
                    cell.transform.localRotation = Quaternion.identity;
                    cell.transform.localScale = new Vector3(0.39f, 0.39f, 1f);
                    cell.transform.localPosition = new Vector3(
                        col * cellSize - offset,
                        row * cellSize - offset,
                        0f);
                    cell.Initialize(row, col, cellColor);
                    cell.gameObject.name = $"Cell_{row}_{col}";
                    _cells[row, col] = cell;
                }
            }
        }

        private void PlaceInitialPieces()
        {
            if (_playerCount == 2) PlacePiecesForTwoPlayers();
            else if (_playerCount == 4) PlacePiecesForFourPlayers();
        }

        private void PlacePiecesForTwoPlayers()
        {
            int rows = (_boardSize / 2) - 1;

            for (int row = 0; row < rows; row++)
                for (int col = 0; col < _boardSize; col++)
                    if ((row + col) % 2 == 1) PlacePiece(row, col, 1);

            for (int row = _boardSize - rows; row < _boardSize; row++)
                for (int col = 0; col < _boardSize; col++)
                    if ((row + col) % 2 == 1) PlacePiece(row, col, 2);
        }

        private void PlacePiecesForFourPlayers()
        {
            int q    = Mathf.Max(1, _boardSize / 4);
            int half = _boardSize / 2;

            for (int row = 0; row < q; row++)
                for (int col = 0; col < half; col++)
                    if ((row + col) % 2 == 1) PlacePiece(row, col, 1);

            for (int row = _boardSize - q; row < _boardSize; row++)
                for (int col = half; col < _boardSize; col++)
                    if ((row + col) % 2 == 1) PlacePiece(row, col, 2);

            for (int row = _boardSize - q; row < _boardSize; row++)
                for (int col = 0; col < half; col++)
                    if ((row + col) % 2 == 1) PlacePiece(row, col, 3);

            for (int row = 0; row < q; row++)
                for (int col = half; col < _boardSize; col++)
                    if ((row + col) % 2 == 1) PlacePiece(row, col, 4);
        }

        #endregion

        #region Board Operations

        public void PlacePiece(int row, int col, int playerOwner)
        {
            if (!IsInBounds(row, col)) return;
            _boardState[row, col] = new PieceData(playerOwner, false);
            CreateVisualPiece(row, col, playerOwner, false);
        }

        public void MovePiece(int fromRow, int fromCol, int toRow, int toCol)
        {
            if (!IsInBounds(fromRow, fromCol) || !IsInBounds(toRow, toCol)) return;

            PieceData piece = _boardState[fromRow, fromCol];
            if (piece.IsEmpty) return;

            _boardState[toRow, toCol]   = piece;
            _boardState[fromRow, fromCol] = PieceData.Empty;

            CheckersPiece visual = _pieceObjects[fromRow, fromCol];
            if (visual != null)
            {
                visual.MoveTo(toRow, toCol);
                _pieceObjects[toRow, toCol]   = visual;
                _pieceObjects[fromRow, fromCol] = null;
            }

            OnPieceMoved?.Invoke(fromRow, fromCol, toRow, toCol);
        }

        public void CapturePiece(int row, int col)
        {
            if (!IsInBounds(row, col) || _boardState[row, col].IsEmpty) return;

            _boardState[row, col] = PieceData.Empty;

            CheckersPiece visual = _pieceObjects[row, col];
            if (visual != null)
            {
                visual.ReturnToPool();
                _piecePool.ReturnPiece(visual);
                _pieceObjects[row, col] = null;
            }

            OnPieceCaptured?.Invoke(row, col);
        }

        public void PromoteToKing(int row, int col)
        {
            if (!IsInBounds(row, col) || _boardState[row, col].IsEmpty) return;

            PieceData p = _boardState[row, col];
            p.isKing = true;
            _boardState[row, col] = p;

            _pieceObjects[row, col]?.PromoteToKing();
            OnPiecePromoted?.Invoke(row, col);
        }

        #endregion

        #region Move Queries

        public List<MoveData> GetValidMoves(int row, int col)
        {
            _moveCache.Clear();
            if (!IsInBounds(row, col) || _boardState[row, col].IsEmpty) return _moveCache;
            EnsureConfigs();
            _moveCache.AddRange(CheckersRules.GetValidMovesForPiece(_boardState, row, col, _gameSettings));
            return _moveCache;
        }

        public List<MoveData> GetAllValidMovesForPlayer(int playerOwner)
        {
            _allMovesCache.Clear();
            EnsureConfigs();

            for (int r = 0; r < _boardSize; r++)
                for (int c = 0; c < _boardSize; c++)
                    if (_boardState[r, c].playerOwner == playerOwner)
                        _allMovesCache.AddRange(
                            CheckersRules.GetValidMovesForPiece(_boardState, r, c, _gameSettings));

            if (_gameSettings.forceCapture)
            {
                bool hasCapture = false;
                for (int i = 0; i < _allMovesCache.Count; i++)
                    if (_allMovesCache[i].isCapture) { hasCapture = true; break; }

                if (hasCapture)
                    for (int i = _allMovesCache.Count - 1; i >= 0; i--)
                        if (!_allMovesCache[i].isCapture) _allMovesCache.RemoveAt(i);
            }

            return _allMovesCache;
        }

        #endregion

        #region Serialization

        public string SerializeBoardState()
        {
            int turnIndex = GameManager.Instance?.TurnManager?.CurrentTurnIndex ?? 0;
            return StateSerializer.SerializeBoard(_boardState, turnIndex);
        }

        public void DeserializeBoardState(string json)
        {
            EnsureConfigs();

            PieceData[,] restored = StateSerializer.DeserializeBoard(json, out int turnIndex);
            if (restored == null) { Debug.LogError("[BoardManager] Deserialize failed."); return; }

            int size = restored.GetLength(0);
            _boardSize   = size;
            _boardState  = restored;
            _cells        = new BoardCell[size, size];
            _pieceObjects = new CheckersPiece[size, size];

            ClearBoard();
            CreateVisualBoard();

            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    if (!_boardState[r, c].IsEmpty)
                        CreateVisualPiece(r, c, _boardState[r, c].playerOwner, _boardState[r, c].isKing);
        }

        #endregion

        #region Helpers

        private bool IsInBounds(int r, int c) =>
            r >= 0 && r < _boardSize && c >= 0 && c < _boardSize;

        private void CreateVisualPiece(int row, int col, int playerOwner, bool isKing)
        {
            EnsureConfigs();

            CheckersPiece piece;

            if (piecePrefab != null && _piecePool != null)
            {
                piece = _piecePool.GetPiece();
            }
            else
            {
                GameObject go = new GameObject($"Piece_P{playerOwner}_{row}_{col}");
                go.AddComponent<SpriteRenderer>().sortingOrder = 1;
                piece = go.AddComponent<CheckersPiece>();
            }

            if (piece == null) return;

            float cellSize = _boardConfig.cellSize > 0 ? _boardConfig.cellSize : 1f;
            float offset   = (_boardSize * cellSize) / 2f - cellSize / 2f;

            piece.transform.SetParent(boardParent);
            // CRITICAL: Reset transform completely before setting new values
            piece.transform.localRotation = Quaternion.identity;
            piece.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
            piece.transform.localPosition = new Vector3(
                col * cellSize - offset,
                row * cellSize - offset,
                -0.1f);

            piece.Initialize(playerOwner, row, col, _pieceConfig);
            if (isKing) piece.PromoteToKing();

            piece.gameObject.SetActive(true);
            piece.gameObject.name = $"Piece_P{playerOwner}_{row}_{col}";
            _pieceObjects[row, col] = piece;
        }

        public CheckersPiece GetPieceAt(int row, int col) =>
            IsInBounds(row, col) ? _pieceObjects[row, col] : null;

        public BoardCell GetCellAt(int row, int col) =>
            IsInBounds(row, col) ? _cells[row, col] : null;

        public int GetPieceCount(int playerOwner)
        {
            int count = 0;
            for (int r = 0; r < _boardSize; r++)
                for (int c = 0; c < _boardSize; c++)
                    if (_boardState[r, c].playerOwner == playerOwner) count++;
            return count;
        }

        public Vector3 GetWorldPosition(int row, int col)
        {
            float cellSize = _boardConfig?.cellSize > 0 ? _boardConfig.cellSize : 1f;
            float offset   = (_boardSize * cellSize) / 2f - cellSize / 2f;
            return new Vector3(col * cellSize - offset, row * cellSize - offset, 0f);
        }

        #endregion
    }
}   