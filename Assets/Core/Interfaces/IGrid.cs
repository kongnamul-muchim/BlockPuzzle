using System.Collections.Generic;

namespace BlockPuzzle.Core.Interfaces
{
    /// <summary>
    /// 제거 결과: 제거된 블럭 목록 + 각 블럭의 낙하 거리
    /// </summary>
    public class RemovalResult
    {
        public List<IBlock> RemovedBlocks { get; } = new();
        public Dictionary<IBlock, int> FallDistances { get; } = new(); // 블럭별 낙하 칸 수
        public int ClearedCount => RemovedBlocks.Count;
    }

    public interface IGrid
    {
        int Rows { get; }          // 10
        int Columns { get; }       // 10

        /// <summary>특정 좌표의 블럭 조회 (null 가능)</summary>
        IBlock GetBlockAt(int row, int column);

        /// <summary>모든 블럭을 열거 (null 제외)</summary>
        IEnumerable<IBlock> GetAllBlocks();

        /// <summary>하단(10행)에 새 행 추가, 기존 블럭을 위로 1칸 밀어올림</summary>
        /// <returns>밀려난 후 1행 위로 넘어간 블럭이 있으면 true (게임오버)</returns>
        bool AddRowAtBottom();

        /// <summary>제거할 블럭들을 격자에서 제거</summary>
        void RemoveBlocks(IReadOnlyList<IBlock> blocks);

        /// <summary>중력 적용: 제거된 자리로 위 블럭 낙하</summary>
        /// <returns>낙하 결과 (떨어진 블럭과 거리)</returns>
        RemovalResult ApplyGravity();

        /// <summary>빈 열을 중앙 방향으로 이동</summary>
        void ShiftColumnsTowardCenter();

        /// <summary>제거 횟수 누적 (턴 시스템용)</summary>
        int RemovalCount { get; }

        /// <summary>제거 횟수 초기화 (턴 리셋)</summary>
        void ResetRemovalCount();

        /// <summary>지정 열이 전부 비었는지 확인</summary>
        bool IsColumnEmpty(int column);

        /// <summary>초기 블럭 배치 (게임 시작 시 일부 채움)</summary>
        void Initialize();

        /// <summary>블럭이 격자 상단(1행 위)으로 넘어갔는지 확인 (게임오버 조건)</summary>
        bool HasOverflow();
    }
}
