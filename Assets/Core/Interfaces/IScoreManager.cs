namespace BlockPuzzle.Core.Interfaces
{
    public class ScoreBreakdown
    {
        public int BlockCount { get; set; }
        public int BaseScore { get; set; }          // blockCount × 10
        public double Multiplier { get; set; }       // 연계 배율
        public int ChainScore { get; set; }          // BaseScore × Multiplier
        public int FallBonus { get; set; }           // 낙차 보너스
        public int TotalScore { get; set; }          // ChainScore + FallBonus

        public override string ToString()
        {
            return $"{BlockCount}블럭 ×{Multiplier:F1} + 낙차{FallBonus} = {TotalScore}점";
        }
    }

    public interface IScoreManager
    {
        /// <summary>현재 누적 점수</summary>
        int CurrentScore { get; }

        /// <summary>현재 게임의 최대 연계 수</summary>
        int MaxCombo { get; }

        /// <summary>제거한 총 블럭 수</summary>
        int TotalClearedBlocks { get; }

        /// <summary>블럭 제거 점수 계산 및 누적</summary>
        ScoreBreakdown CalculateScore(int blockCount, int fallBonus);

        /// <summary>점수 초기화 (새 게임)</summary>
        void Reset();
    }
}
