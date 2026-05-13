using BlockPuzzle.Core.Interfaces;

namespace BlockPuzzle.Core.Game
{
    public class ScoreManager : IScoreManager
    {
        public const int BASE_SCORE_PER_BLOCK = 10;
        public const int FALL_BONUS_PER_CELL = 5;

        public int CurrentScore { get; private set; }
        public int MaxCombo { get; private set; }
        public int TotalClearedBlocks { get; private set; }

        public ScoreManager()
        {
        }

        public ScoreBreakdown CalculateScore(int blockCount, int fallBonus)
        {
            if (blockCount <= 0)
                return new ScoreBreakdown();

            double multiplier = GetMultiplier(blockCount);
            int baseScore = blockCount * BASE_SCORE_PER_BLOCK;
            int chainScore = (int)(baseScore * multiplier);
            int totalScore = chainScore + fallBonus;

            var breakdown = new ScoreBreakdown
            {
                BlockCount = blockCount,
                BaseScore = baseScore,
                Multiplier = multiplier,
                ChainScore = chainScore,
                FallBonus = fallBonus,
                TotalScore = totalScore
            };

            // 누적
            CurrentScore += totalScore;
            TotalClearedBlocks += blockCount;

            if (blockCount > MaxCombo)
                MaxCombo = blockCount;

            return breakdown;
        }

        /// <summary>
        /// 연계 블럭 수에 따른 배율 반환.
        /// </summary>
        private static double GetMultiplier(int blockCount)
        {
            return blockCount switch
            {
                2 => 1.0,
                3 => 1.0,
                4 => 1.5,
                5 => 2.0,
                6 => 2.5,
                7 => 3.0,
                8 => 4.0,
                9 => 4.5,
                >= 10 => 5.0 + (blockCount - 10) * 0.5,
                _ => 0.0
            };
        }

        /// <summary>
        /// 낙차 보너스 계산 (fallDistance × 5)
        /// </summary>
        public int CalculateFallBonus(int fallDistance)
        {
            return fallDistance * FALL_BONUS_PER_CELL;
        }

        public void Reset()
        {
            CurrentScore = 0;
            MaxCombo = 0;
            TotalClearedBlocks = 0;
        }
    }
}
