using System;
using UnityEngine;
using Checkers.Core;
using Checkers.Utilities;
namespace Checkers.Network
{
    public static class StateSerializer
    {
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
                        snapshot.cells[index] = piece.playerOwner * 10 + (piece.isKing ? 1 : 0);
                    }
                }
            }
            string json = JsonUtility.ToJson(snapshot);
            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Board serialized. Size: {size}, Turn: {currentTurnIndex}, JSON length: {json.Length}");
            return json;
        }
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
