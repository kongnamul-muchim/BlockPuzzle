using BlockPuzzle.Core.Interfaces;
using BlockPuzzle.Core.Managers;
using BlockPuzzle.Core.Utilities;
using UnityEngine;

namespace BlockPuzzle.Unity.Adapters
{
    /// <summary>
    /// Unity 전용 GameplayLogger 어댑터.
    /// 하는 일: 1) Core 로거 생성 2) Debug.Log 캡처 연결 3) 씬 라이프사이클 관리
    /// 코어 로직은 전부 Core/Utilities/GameplayLogger.cs (순수 C#).
    /// </summary>
    public class GameplayLogger : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _logToFile = true;
        [SerializeField] private string _logDirectoryName = "logs";

        private Core.Utilities.GameplayLogger _coreLogger;

        private void Awake()
        {
            if (!_logToFile) return;

            // 로그 디렉토리 경로 (Unity 의존: Application.dataPath)
            string projectPath = Application.dataPath.Replace("/Assets", "").Replace("\\Assets", "");
            string logDir = System.IO.Path.Combine(projectPath, "progress", _logDirectoryName);

            // 순수 C# 로거 생성 (디렉토리 기반 → GAME.md, INFO.md, WARN.md, ERROR.md 자동 생성)
            _coreLogger = new Core.Utilities.GameplayLogger(logDir, includeTimestamp: true);

            // 게임 이벤트 연결
            if (GameManager.Container != null)
            {
                var stateMachine = GameManager.Container.Resolve<IGameStateMachine>();
                _coreLogger.SubscribeToGameEvents(stateMachine);

                // 씬 전환 후 로드: 이미 Playing 상태면 세션 시작
                if (stateMachine.CurrentState == GameState.Playing)
                {
                    _coreLogger.StartNewSession();
                    _coreLogger.AppendToCategory("GAME", "### Scene loaded (mid-game)\n");
                }
            }

            // Unity Debug.Log 캡처
            Application.logMessageReceived += OnUnityLog;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnUnityLog;

            if (_coreLogger != null)
            {
                _coreLogger.UnsubscribeFromGameEvents();
                _coreLogger.FlushToFile();
            }
        }

        private void OnUnityLog(string logString, string stackTrace, LogType type)
        {
            if (_coreLogger == null) return;

            string logType = type switch
            {
                LogType.Error     => "Error",
                LogType.Assert    => "Assert",
                LogType.Warning   => "Warning",
                LogType.Exception => "Exception",
                _                 => "Log"
            };

            _coreLogger.LogExternal(logString, stackTrace, logType);
        }

        /// <summary>
        /// 특정 카테고리의 전체 로그 내용 반환.
        /// </summary>
        public string GetLog(string category = "GAME")
        {
            return _coreLogger?.GetLog(category) ?? "";
        }
    }
}
