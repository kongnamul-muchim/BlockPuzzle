using System;
using System.IO;
using System.Text;
using BlockPuzzle.Core.Interfaces;
using BlockPuzzle.Core.Managers;
using UnityEngine;

namespace BlockPuzzle.Unity.Adapters
{
    /// <summary>
    /// 게임 플레이 로그 자동 기록.
    /// 콘솔 로그 + 게임 이벤트를 문서화하여 저장.
    /// 새 게임 시작 시 이전 로그 초기화.
    /// </summary>
    public class GameplayLogger : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _logToFile = true;
        [SerializeField] private bool _includeTimestamp = true;
        [SerializeField] private string _fileName = "gameplay-log.txt";

        private StringBuilder _logBuffer;
        private string _filePath;
        private IGameStateMachine _stateMachine;

        private const string LOG_SEPARATOR = "──────────────────────────────────────────────";

        private void Awake()
        {
            _logBuffer = new StringBuilder();

            // 저장 경로: 프로젝트 루트의 progress/ 폴더
            string projectPath = Application.dataPath.Replace("/Assets", "").Replace("\\Assets", "");
            string progressDir = Path.Combine(projectPath, "progress");
            if (!Directory.Exists(progressDir))
                Directory.CreateDirectory(progressDir);

            _filePath = Path.Combine(progressDir, _fileName);

            if (GameManager.Container != null)
            {
                _stateMachine = GameManager.Container.Resolve<IGameStateMachine>();
            }

            if (_stateMachine != null)
            {
                _stateMachine.OnStateChanged += OnStateChanged;
                _stateMachine.OnScoreChanged += OnScoreChanged;
                _stateMachine.OnBlocksRemoved += OnBlocksRemoved;
                _stateMachine.OnGameOver += OnGameOver;
                _stateMachine.OnRowAdded += OnRowAdded;
            }

            // Unity 콘솔 로그 캡처
            if (_logToFile)
                Application.logMessageReceived += OnLogMessage;
        }

        private void OnDestroy()
        {
            if (_logToFile)
                Application.logMessageReceived -= OnLogMessage;

            if (_stateMachine != null)
            {
                _stateMachine.OnStateChanged -= OnStateChanged;
                _stateMachine.OnScoreChanged -= OnScoreChanged;
                _stateMachine.OnBlocksRemoved -= OnBlocksRemoved;
                _stateMachine.OnGameOver -= OnGameOver;
                _stateMachine.OnRowAdded -= OnRowAdded;
            }

            // 남은 버퍼 저장
            FlushToFile();
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
                    AppendLog("[Game] Game Over triggered");
                    FlushToFile();
                    break;

                case GameState.MainMenu:
                    AppendLog("[Game] Returned to Main Menu");
                    break;
            }
        }

        private void OnScoreChanged(ScoreBreakdown score)
        {
            AppendLog($"[Score] +{score.TotalScore} ({score.BlockCount} blocks ×{score.Multiplier:F1} + fall {score.FallBonus})");
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
            AppendLog($"[GameOver] Score: {data.FinalScore}, MaxCombo: {data.MaxCombo}, Cleared: {data.TotalClearedBlocks}, Diff: {data.Difficulty}");
        }

        /// <summary>
        /// 새 게임 세션 시작 — 이전 로그 초기화.
        /// </summary>
        private void StartNewSession()
        {
            _logBuffer.Clear();
            _logBuffer.AppendLine("╔══════════════════════════════════════════╗");
            _logBuffer.AppendLine("║        ChainCrush Gameplay Log           ║");
            _logBuffer.AppendLine("╚══════════════════════════════════════════╝");
            AppendTimestamp($"Session started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _logBuffer.AppendLine(LOG_SEPARATOR);

            // 파일 초기화 (덮어쓰기)
            try
            {
                File.WriteAllText(_filePath, _logBuffer.ToString());
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameplayLogger] Failed to write log file: {e.Message}");
            }
        }

        private void OnLogMessage(string logString, string stackTrace, LogType type)
        {
            string prefix = type switch
            {
                LogType.Error => "[ERR]",
                LogType.Assert => "[ASSERT]",
                LogType.Warning => "[WARN]",
                LogType.Exception => "[EXCEPTION]",
                _ => "[LOG]"
            };

            AppendLog($"{prefix} {logString}");

            // 에러/예외는 스택 트레이스도 기록
            if (type == LogType.Error || type == LogType.Exception)
            {
                AppendLog($"  └─ {stackTrace}");
            }
        }

        private void AppendLog(string message)
        {
            if (!_logToFile) return;

            string entry = _includeTimestamp
                ? $"[{DateTime.Now:HH:mm:ss.fff}] {message}"
                : message;

            _logBuffer.AppendLine(entry);
        }

        private void AppendTimestamp(string message)
        {
            _logBuffer.AppendLine(message);
        }

        private void FlushToFile()
        {
            if (!_logToFile || _logBuffer == null || _logBuffer.Length == 0)
                return;

            try
            {
                File.WriteAllText(_filePath, _logBuffer.ToString());
                Debug.Log($"[GameplayLogger] Log saved → {_filePath}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameplayLogger] Flush failed: {e.Message}");
            }
        }

        /// <summary>
        /// 수동으로 로그 저장 (언제든 호출 가능).
        /// </summary>
        public void SaveNow()
        {
            FlushToFile();
        }
    }
}
