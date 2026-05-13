using BlockPuzzle.Core.Interfaces;
using BlockPuzzle.Core.Managers;
using BlockPuzzle.Core.Utilities;
using UnityEngine;

namespace BlockPuzzle.Unity.Adapters
{
    public class GameplayLogger : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _logToFile = true;
        [SerializeField] private string _logDirectoryName = "logs";

        private Core.Utilities.GameplayLogger _coreLogger;
        private IGameStateMachine _stateMachine;

        private void Awake()
        {
            if (!_logToFile) return;

            // 씬 재로드 시 중복 방지
            var existing = FindAnyObjectByType<GameplayLogger>();
            if (existing != null && existing != this)
            {
                Destroy(gameObject);
                return;
            }

            // 씬 전환 시 파괴 방지 (모든 씬에서 로그 지속)
            DontDestroyOnLoad(gameObject);

            string projectPath = Application.dataPath.Replace("/Assets", "").Replace("\\Assets", "");
            string logDir = System.IO.Path.Combine(projectPath, "progress", _logDirectoryName);

            _coreLogger = new Core.Utilities.GameplayLogger(logDir, includeTimestamp: true);

            Application.logMessageReceived += OnUnityLog;

            TryResolveDependencies();
        }

        private void Start()
        {
            if (_stateMachine == null)
                TryResolveDependencies();
        }

        private void TryResolveDependencies()
        {
            if (GameManager.Container != null)
            {
                _stateMachine = GameManager.Container.Resolve<IGameStateMachine>();
            }

            if (_stateMachine == null)
                return;

            _coreLogger.SubscribeToGameEvents(_stateMachine);

            if (_stateMachine.CurrentState == GameState.Playing)
            {
                _coreLogger.StartNewSession();
                _coreLogger.AppendToCategory("GAME", "### Scene loaded (mid-game)\n");
            }

            _coreLogger.AppendToCategory("GAME", "### Logger initialized\n");
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

        public string GetLog(string category = "GAME")
        {
            return _coreLogger?.GetLog(category) ?? "";
        }
    }
}
