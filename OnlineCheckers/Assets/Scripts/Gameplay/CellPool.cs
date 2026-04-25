using UnityEngine;
namespace Checkers.Gameplay
{
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