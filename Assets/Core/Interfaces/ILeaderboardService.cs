using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlockPuzzle.Core.Interfaces
{
    /// <summary>
    /// 리더보드 API 추상화 인터페이스.
    /// </summary>
    public class LeaderboardEntry
    {
        public string PlayerName { get; set; }
        public int Score { get; set; }
        public int MaxCombo { get; set; }
        public int TotalCleared { get; set; }
        public string Difficulty { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? Rank { get; set; }
    }

    public interface ILeaderboardService
    {
        /// <summary>랭킹 조회 (상위 100)</summary>
        Task<List<LeaderboardEntry>> GetLeaderboardAsync(string difficulty = null);

        /// <summary>점수 저장</summary>
        Task<bool> SaveScoreAsync(LeaderboardEntry entry);
    }
}
