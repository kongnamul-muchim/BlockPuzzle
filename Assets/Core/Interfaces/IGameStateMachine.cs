using System;
using System.Collections.Generic;

namespace BlockPuzzle.Core.Interfaces
{
    public enum GameState
    {
        MainMenu,
        Playing,
        GameOver
    }

    public class GameOverData
    {
        public int FinalScore { get; set; }
        public int MaxCombo { get; set; }
        public int TotalClearedBlocks { get; set; }
        public Difficulty Difficulty { get; set; }
        public int GameDurationSeconds { get; set; }
    }

    public interface IGameStateMachine
    {
        GameState CurrentState { get; }

        /// <summary>게임 상태 변경 이벤트</summary>
        event Action<GameState> OnStateChanged;

        /// <summary>점수 변경 이벤트</summary>
        event Action<ScoreBreakdown> OnScoreChanged;

        /// <summary>블럭 제거 이벤트</summary>
        event Action<IReadOnlyList<IBlock>> OnBlocksRemoved;

        /// <summary>중력 적용 이벤트 (낙하)</summary>
        event Action<RemovalResult> OnGravityApplied;

        /// <summary>열 이동 이벤트</summary>
        event Action OnColumnsShifted;

        /// <summary>새 행 추가 이벤트 (턴)</summary>
        event Action OnRowAdded;

        /// <summary>게임오버 이벤트</summary>
        event Action<GameOverData> OnGameOver;

        /// <summary>메인 메뉴에서 난이도 선택 후 게임 시작</summary>
        void StartGame(Difficulty difficulty);

        /// <summary>플레이어가 블럭 클릭 (격자 좌표)</summary>
        void ProcessClick(int row, int column);

        /// <summary>게임오버 화면에서 메인 메뉴로</summary>
        void GoToMainMenu();

        /// <summary>게임오버 화면에서 재시작</summary>
        void RestartGame();

        /// <summary>현재 게임 통계 반환 (GameOver 시점)</summary>
        GameOverData GetGameOverData();
    }
}
