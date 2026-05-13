using BlockPuzzle.Core.Game;

namespace BlockPuzzle.Core.Interfaces
{
    public enum BlockState
    {
        /// <summary>격자에 정상 위치</summary>
        Normal,
        /// <summary>플레이어가 선택 중</summary>
        Selected,
        /// <summary>제거 중 (애니메이션)</summary>
        Removing,
        /// <summary>제거됨</summary>
        Removed,
        /// <summary>낙하 중</summary>
        Falling
    }

    public interface IBlock
    {
        BlockColor Color { get; }
        int Row { get; }
        int Column { get; }
        BlockState State { get; set; }

        void MoveTo(int row, int column);
        IBlock Clone();
    }
}
