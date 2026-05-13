using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BlockPuzzle.Core.Interfaces;

namespace BlockPuzzle.Core.Utilities
{
    /// <summary>
    /// 순수 C# 게임플레이 로거.
    /// 태그별(GAME/INFO/WARN/ERROR) 마크다운 파일로 분산 기록.
    /// 파일 I/O 전부 순수 C#으로 처리.
    /// </summary>
    public class GameplayLogger
    {
        private readonly Dictionary<string, StringBuilder> _buffers = new();
        private readonly string _logDirectory;
        private readonly bool _includeTimestamp;
        private IGameStateMachine _stateMachine;
        private bool _isStarted = true; // 기본 활성화 (메뉴 로그도 캡처)

        private static readonly string[] Categories = { "GAME", "INFO", "WARN", "ERROR" };

        /// <summary>
        /// 외부 로그 수신용 이벤트 (Unity Debug.Log 등).
        /// </summary>
        public event Action<string, string, string> OnExternalLog;

        /// <summary>
        /// 로그 출력 이벤트 (실시간 UI 표시 등).
        /// </summary>
        public event Action<string, string> OnLogLine; // message, category

        /// <summary>
        /// 특정 카테고리의 전체 로그 내용.
        /// </summary>
        public string GetLog(string category)
        {
            return _buffers.TryGetValue(category, out var sb) ? sb.ToString() : "";
        }

        public GameplayLogger(string logDirectory, bool includeTimestamp = true)
        {
            _logDirectory = logDirectory ?? throw new ArgumentNullException(nameof(logDirectory));
            _includeTimestamp = includeTimestamp;

            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);

            // 시작부터 로그 캡처 가능하도록 버퍼 초기화
            InitializeBuffers();
        }

        public void SubscribeToGameEvents(IGameStateMachine stateMachine)
        {
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));

            _stateMachine.OnStateChanged += OnStateChanged;
            _stateMachine.OnScoreChanged += OnScoreChanged;
            _stateMachine.OnBlocksRemoved += OnBlocksRemoved;
            _stateMachine.OnRowAdded += OnRowAdded;
            _stateMachine.OnGameOver += OnGameOver;
        }

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

        // ───── 게임 이벤트 → GAME 카테고리 ─────

        private void OnStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Playing:
                    StartNewSession();
                    AppendGameLog("## Game Started\n");
                    break;

                case GameState.GameOver:
                    AppendGameLog("## Game Over\n");
                    FlushToFile();
                    break;

                case GameState.MainMenu:
                    AppendGameLog("## Returned to Main Menu\n");
                    break;
            }
        }

        private void OnScoreChanged(ScoreBreakdown score)
        {
            AppendGameLog(
                $"### Score +{score.TotalScore}\n" +
                $"- Blocks: {score.BlockCount}\n" +
                $"- Multiplier: {score.Multiplier:F1}\n" +
                $"- Fall Bonus: {score.FallBonus}\n");
        }

        private void OnBlocksRemoved(IReadOnlyList<IBlock> blocks)
        {
            AppendGameLog($"### Clear: {blocks.Count} blocks removed\n");
        }

        private void OnRowAdded()
        {
            AppendGameLog("### New row added (bottom)\n");
        }

        private void OnGameOver(GameOverData data)
        {
            AppendGameLog(
                $"### Game Over\n" +
                $"- Final Score: {data.FinalScore}\n" +
                $"- Max Combo: {data.MaxCombo}\n" +
                $"- Total Cleared: {data.TotalClearedBlocks}\n" +
                $"- Difficulty: {data.Difficulty}\n");
        }

        // ───── 세션 관리 ─────

        public void StartNewSession()
        {
            _isStarted = true;
            InitializeBuffers(isNewSession: true);
        }

        private void InitializeBuffers(bool isNewSession = false)
        {
            string sessionLabel = isNewSession
                ? $"Session: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                : $"Pre-game log (auto-initialized)";

            foreach (string cat in Categories)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"# {cat} Log");
                sb.AppendLine();
                sb.AppendLine(sessionLabel);
                sb.AppendLine("---");
                sb.AppendLine();
                _buffers[cat] = sb;
            }
        }

        // ───── 로그 추가 ─────

        public void LogExternal(string logString, string stackTrace, string logType)
        {
            string category = logType switch
            {
                "Warning" => "WARN",
                "Error" or "Exception" => "ERROR",
                _ => "INFO"
            };

            string entry = FormatEntry(logString);
            AppendToCategory(category, entry);

            if (logType == "Error" || logType == "Exception")
            {
                AppendToCategory(category, $"> {stackTrace.Replace("\n", "\n> ")}\n");
            }

            OnExternalLog?.Invoke(logString, stackTrace, logType);
        }

        public void AppendToCategory(string category, string markdownContent)
        {
            if (!_isStarted) return;
            if (!_buffers.ContainsKey(category)) return;

            _buffers[category].AppendLine(markdownContent);
            OnLogLine?.Invoke(markdownContent, category);
        }

        private void AppendGameLog(string markdownContent)
        {
            AppendToCategory("GAME", markdownContent);
        }

        private string FormatEntry(string message)
        {
            return _includeTimestamp
                ? $"**{DateTime.Now:HH:mm:ss.fff}** {message}"
                : message;
        }

        // ───── 파일 I/O ─────

        public void FlushToFile()
        {
            foreach (string cat in Categories)
            {
                if (_buffers.TryGetValue(cat, out var sb) && sb.Length > 0)
                {
                    string filePath = Path.Combine(_logDirectory, $"{cat}.md");
                    TryWriteToFile(filePath, sb.ToString());
                }
            }
        }

        private void TryWriteToFile(string filePath, string content)
        {
            try
            {
                File.WriteAllText(filePath, content);
            }
            catch (Exception e)
            {
                // Fallback: GAME 버퍼에 에러 기록
                if (_buffers.TryGetValue("GAME", out var sb))
                {
                    sb.AppendLine($"> Logger file write failed: {e.Message}");
                }
            }
        }
    }
}
