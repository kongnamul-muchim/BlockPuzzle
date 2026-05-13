using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace BlockPuzzle.Unity.UI
{
    /// <summary>
    /// 메인 스레드 디스패처.
    /// 백그라운드 스레드에서 UI 업데이트가 필요할 때 사용.
    /// 빈 GameObject에 붙여서 사용.
    /// </summary>
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static MainThreadDispatcher _instance;
        private readonly ConcurrentQueue<Action> _actions = new();

        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            while (_actions.TryDequeue(out Action action))
            {
                action?.Invoke();
            }
        }

        /// <summary>
        /// 메인 스레드에서 액션 실행을 예약.
        /// 인스턴스가 없으면 자동 생성하여 안전하게 처리.
        /// </summary>
        public static void ExecuteOnMainThread(Action action)
        {
            if (_instance != null)
            {
                _instance._actions.Enqueue(action);
            }
            else
            {
                // 인스턴스가 없으면 자동 생성 (DontDestroyOnLoad)
                var go = new GameObject("MainThreadDispatcher (Auto-created)");
                _instance = go.AddComponent<MainThreadDispatcher>();
                DontDestroyOnLoad(go);
                _instance._actions.Enqueue(action);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
