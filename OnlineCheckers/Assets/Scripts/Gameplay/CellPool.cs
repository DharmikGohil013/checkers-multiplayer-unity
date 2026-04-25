// Assets/Scripts/Gameplay/CellPool.cs
using UnityEngine;

namespace Checkers.Gameplay
{
    /// <summary>
    /// Object pool for BoardCell GameObjects.
    /// Add this component to GameManagers in the Inspector.
    /// </summary>
    public class CellPool : PoolBase
    {
        public BoardCell GetCell()
        {
            var go = Get();
            return go != null ? go.GetComponent<BoardCell>() : null;
        }

        public void ReturnCell(BoardCell cell)
        {
            if (cell != null) Return(cell.gameObject);
        }
    }
}