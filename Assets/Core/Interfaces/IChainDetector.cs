using System.Collections.Generic;

namespace BlockPuzzle.Core.Interfaces
{
    public interface IChainDetector
    {
        /// <summary>
        /// 지정 좌표에서 시작하여 인접한(상/하/좌/우) 같은 색상 블럭들을 BFS로 탐색.
        /// </summary>
        /// <returns>연결된 블럭 그룹 (최소 2개 이상이어야 제거 가능)</returns>
        IReadOnlyList<IBlock> FindConnectedBlocks(int row, int column);

        /// <summary>
        /// 해당 그룹이 제거 가능한 크기(2개 이상)인지 확인.
        /// </summary>
        bool CanRemove(IReadOnlyList<IBlock> group);

        /// <summary>
        /// 현재 격자에 제거 가능한 블럭 그룹이 하나라도 있는지 전체 탐색.
        /// 없으면 교착 상태(deadlock) → 강제 턴 진행 필요.
        /// </summary>
        bool HasAnyValidMove();
    }
}
