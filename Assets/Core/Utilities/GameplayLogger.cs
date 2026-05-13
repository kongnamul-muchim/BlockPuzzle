using System;
using System.IO;
using System.Text;
using BlockPuzzle.Core.Interfaces;

namespace BlockPuzzle.Core.Utilities
{
    /// <summary>
    /// 순수 C# 게임플레이 로거.
    /// 콘솔 로그 제외한 모든 게임 이벤트(점수/제거/턴/게임오버) 기록.
    /// 파일 I/O 전부 순수 C#으로 처리.
    /// </summary>
    public class GameplayLogger
    {
        private readonly StringBuilder _logBuffer = new();
        private readonly string _filePath;
        private readonly bool _includeTimestamp;
        private IGameStateMachine _stateMachine;
        private bool _isStarted;

        private const string SEPARATOR = "──────────────────────────────────────────────";

        /// <summary>
        /// 외부 로그 수신용 이벤트 (Unity Debug.Log 등).
        /// 포맷: logString, stackTrace, LogType
        /// </summary>
        public event Action<string, string, string> OnExternalLog;

        /// <summary>
        /// 로그 출력 이벤트 (실시간 UI 표시 등에 사용).
        /// </summary>
        public event Action<string> OnLogLine;

        /// <summary>
        /// 현재까지의 로그 전체 내용.
        /// </summary>
        public string FullLog => _logBuffer.ToString();

        public GameplayLogger(string filePath, bool includeTimestamp = true)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _includeTimestamp = includeTimestamp;

            // 디렉토리 없으면 생성
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        /// <summary>
        /// 게임 상태 머신 연결 (로거 활성화).
        /// </summary>
        public void SubscribeToGameEvents(IGameStateMachine stateMachine)
        {
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));

            _stateMachine.OnStateChanged += OnStateChanged;
            _stateMachine.OnScoreChanged += OnScoreChanged;
            _stateMachine.OnBlocksRemoved += OnBlocksRemoved;
            _stateMachine.OnRowAdded += OnRowAdded;
            _stateMachine.OnGameOver += OnGameOver;
        }

        /// <summary>
        /// 게임 상태 머신 연결 해제.
        /// </summary>
        public void UnsubscribeFromGameEvents()
        {
            if (_stateMachine == null) return;

            _stateMachine.OnStateChanged -= OnStateChanged;
            _stateMachine.OnScoreChanged -= OnScoreChanged;
            _stateMachine.OnBlocksRemoved -= OnBlocksRemoved;
            _stateMachine.OnRowAdded -= OnRowAdded;
            _stateMachine.OnGameOver -= OnGameOver;

            _stateMachine = null;
        }

        private void OnStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Playing:
                    StartNewSession();
                    AppendLog("[Game] Game Started");
                    break;

                case GameState.GameOver:
                    AppendLog("[Game] Game Over");
                    FlushToFile();
                    break;

                case GameState.MainMenu:
                    AppendLog("[Game] Returned to Main Menu");
                    break;
            }
        }

        private void OnScoreChanged(ScoreBreakdown score)
        {
            AppendLog($"[Score] +{score.TotalScore} ({score.BlockCount}blk ×{score.Multiplier:F1} + fall {score.FallBonus})");
        }

        private void OnBlocksRemoved(IReadOnlyList<IBlock> blocks)
        {
            AppendLog($"[Clear] {blocks.Count} blocks removed");
        }

        private void OnRowAdded()
        {
            AppendLog("[Turn] New row added at bottom");
        }

        private void OnGameOver(GameOverData data)
        {
            AppendLog($"[GameOver] Score:{data.FinalScore} MaxCombo:{data.MaxCombo} Cleared:{data.TotalClearedBlocks} Diff:{data.Difficulty}");
        }

        /// <summary>
        /// 새 게임 세션 시작 — 이전 로그 초기화.
        /// </summary>
        public void StartNewSession()
        {
            _logBuffer.Clear();
            AppendHeader();
            AppendTimestamp($"Session started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _logBuffer.AppendLine(SEPARATOR);
            _isStarted = true;

            // 파일 덮어쓰기 (초기화)
            TryWriteToFile(_logBuffer.ToString());
        }

        /// <summary>
        /// 외부 로그 메시지 추가 (Unity Debug.Log 등에서 호출).
        /// </summary>
        public void LogExternal(string logString, string stackTrace, string logType)
        {
            AppendLog($"[{logType}] {logString}");

            if (logType == "Error" || logType == "Exception")
            {
                AppendLog($"  └─ {stackTrace}");
            }

            OnExternalLog?.Invoke(logString, stackTrace, logType);
        }

        /// <summary>
        /// 직접 로그 메시지 추가.
        /// </summary>
        public void AppendLog(string message)
        {
            if (!_isStarted) return;

            string entry = _includeTimestamp
                ? $"[{DateTime.Now:HH:mm:ss.fff}] {message}"
                : message;

            _logBuffer.AppendLine(entry);
            OnLogLine?.Invoke(entry);
        }

        /// <summary>
        /// 버퍼를 파일에 저장.
        /// </summary>
        public void FlushToFile()
        {
            if (_logBuffer.Length == 0) return;
            TryWriteToFile(_logBuffer.ToString());
        }

        private void AppendHeader()
        {
            _logBuffer.AppendLine("╔══════════════════════════════════════════╗");
            _logBuffer.AppendLine("║        ChainCrush Gameplay Log           ║");
            _logBuffer.AppendLine("╚══════════════════════════════════════════╝");
        }

        private void AppendTimestamp(string message)
        {
            _logBuffer.AppendLine(message);
        }

        private void TryWriteToFile(string content)
        {
            try
            {
                File.WriteAllText(_filePath, content);
            }
            catch (Exception e)
            {
                AppendLog($"[Logger] Failed to write: {e.Message}");
            }
        }
    }
}
