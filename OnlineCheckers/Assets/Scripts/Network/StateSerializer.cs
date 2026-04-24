using System;
using UnityEngine;
using Checkers.Core;
using Checkers.Utilities;

namespace Checkers.Network
{
    /// <summary>
    /// Static utility class for serializing and deserializing board state.
    /// Uses a flat array encoding compatible with JsonUtility.
    /// </summary>
    public static class StateSerializer
    {
        /// <summary>
        /// Serializable snapshot of the board state.
        /// Uses a flattened 1D array for JsonUtility compatibility.
        /// Encoding: each cell = (playerOwner * 10) + (isKing ? 1 : 0). Empty cells = 0.
        /// </summary>
        [Serializable]
        private class BoardSnapshot
        {
            public int[] cells;
            public int size;
            public int turnIndex;

            public BoardSnapshot() { }

            public BoardSnapshot(int boardSize, int turn)
            {
                size = boardSize;
                turnIndex = turn;
                cells = new int[boardSize * boardSize];
            }
        }

        /// <summary>
        /// Serializes a 2D board state and turn index into a JSON string.
        /// </summary>
        /// <param name="board">The 2D board array.</param>
        /// <param name="currentTurnIndex">The current turn index.</param>
        /// <returns>JSON string representation of the board.</returns>
        public static string SerializeBoard(PieceData[,] board, int currentTurnIndex)
        {
            if (board == null)
            {
                GameLogger.Log(GameLogger.LogLevel.ERROR, "SerializeBoard called with null board.");
                return string.Empty;
            }

            int size = board.GetLength(0);
            BoardSnapshot snapshot = new BoardSnapshot(size, currentTurnIndex);

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    int index = r * size + c;
                    PieceData piece = board[r, c];

                    if (piece.IsEmpty)
                    {
                        snapshot.cells[index] = 0;
                    }
                    else
                    {
                        // Encode: playerOwner * 10 + (isKing ? 1 : 0)
                        snapshot.cells[index] = piece.playerOwner * 10 + (piece.isKing ? 1 : 0);
                    }
                }
            }

            string json = JsonUtility.ToJson(snapshot);

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Board serialized. Size: {size}, Turn: {currentTurnIndex}, JSON length: {json.Length}");

            return json;
        }

        /// <summary>
        /// Deserializes a JSON string back into a 2D board state and turn index.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="currentTurnIndex">Output: the restored turn index.</param>
        /// <returns>The restored 2D board array, or null on failure.</returns>
        public static PieceData[,] DeserializeBoard(string json, out int currentTurnIndex)
        {
            currentTurnIndex = 0;

            if (string.IsNullOrEmpty(json))
            {
                GameLogger.Log(GameLogger.LogLevel.ERROR, "DeserializeBoard called with empty JSON.");
                return null;
            }

            BoardSnapshot snapshot;

            try
            {
                snapshot = JsonUtility.FromJson<BoardSnapshot>(json);
            }
            catch (Exception ex)
            {
                GameLogger.Log(GameLogger.LogLevel.ERROR,
                    $"Failed to deserialize board JSON: {ex.Message}");
                return null;
            }

            if (snapshot == null || snapshot.cells == null)
            {
                GameLogger.Log(GameLogger.LogLevel.ERROR, "Deserialized snapshot is null or has no cells.");
                return null;
            }

            int size = snapshot.size;
            if (size <= 0 || snapshot.cells.Length != size * size)
            {
                GameLogger.Log(GameLogger.LogLevel.ERROR,
                    $"Invalid snapshot: size={size}, cells.Length={snapshot.cells.Length}");
                return null;
            }

            currentTurnIndex = snapshot.turnIndex;
            PieceData[,] board = new PieceData[size, size];

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    int index = r * size + c;
                    int encoded = snapshot.cells[index];

                    if (encoded == 0)
                    {
                        board[r, c] = PieceData.Empty;
                    }
                    else
                    {
                        // Decode: playerOwner = encoded / 10, isKing = (encoded % 10) == 1
                        int playerOwner = encoded / 10;
                        bool isKing = (encoded % 10) == 1;
                        board[r, c] = new PieceData(playerOwner, isKing);
                    }
                }
            }

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Board deserialized. Size: {size}, Turn: {currentTurnIndex}");

            return board;
        }

        /// <summary>
        /// Compares two board states for equality. Used for desync detection.
        /// </summary>
        public static bool AreBoardsEqual(PieceData[,] boardA, PieceData[,] boardB)
        {
            if (boardA == null || boardB == null)
                return false;

            int sizeA = boardA.GetLength(0);
            int sizeB = boardB.GetLength(0);

            if (sizeA != sizeB)
                return false;

            for (int r = 0; r < sizeA; r++)
            {
                for (int c = 0; c < sizeA; c++)
                {
                    PieceData a = boardA[r, c];
                    PieceData b = boardB[r, c];

                    if (a.playerOwner != b.playerOwner || a.isKing != b.isKing)
                    {
                        GameLogger.LogDesync(
                            $"Cell ({r},{c}): Owner={a.playerOwner}, King={a.isKing}",
                            $"Cell ({r},{c}): Owner={b.playerOwner}, King={b.isKing}");
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
