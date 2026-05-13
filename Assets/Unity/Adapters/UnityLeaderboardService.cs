using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlockPuzzle.Core.Interfaces;
using BlockPuzzle.Core.Managers;
using UnityEngine;
using UnityEngine.Networking;

namespace BlockPuzzle.Unity.Adapters
{
    /// <summary>
    /// HTTP 기반 리더보드 서비스.
    /// Vercel API와 통신하여 랭킹 조회/저장.
    /// </summary>
    public class UnityLeaderboardService : MonoBehaviour, ILeaderboardService
    {
        [Header("API Settings")]
        [SerializeField] private string _apiBaseUrl = "https://your-app.vercel.app/api";
        [SerializeField] private float _timeoutSeconds = 10f;

        private void Awake()
        {
            GameManager.RegisterLeaderboardService(this);
        }

        public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(string difficulty = null)
        {
            string url = $"{_apiBaseUrl}/leaderboard";
            if (!string.IsNullOrEmpty(difficulty))
                url += $"?difficulty={UnityWebRequest.EscapeURL(difficulty)}";

            try
            {
                using var request = UnityWebRequest.Get(url);
                request.timeout = Mathf.RoundToInt(_timeoutSeconds);
                request.SetRequestHeader("Accept", "application/json");

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[Leaderboard] GET failed: {request.error}");
                    return new List<LeaderboardEntry>();
                }

                string json = request.downloadHandler.text;
                return ParseLeaderboardJson(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Leaderboard] GET exception: {e.Message}");
                return new List<LeaderboardEntry>();
            }
        }

        public async Task<bool> SaveScoreAsync(LeaderboardEntry entry)
        {
            string url = $"{_apiBaseUrl}/leaderboard";
            var postData = new Dictionary<string, object>
            {
                ["playerName"] = entry.PlayerName,
                ["score"] = entry.Score,
                ["maxCombo"] = entry.MaxCombo,
                ["totalCleared"] = entry.TotalCleared,
                ["difficulty"] = entry.Difficulty,
                ["gameDuration"] = 0
            };

            string jsonPayload = JsonUtility.ToJson(new PostDataWrapper(postData));

            try
            {
                using var request = new UnityWebRequest(url, "POST");
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = Mathf.RoundToInt(_timeoutSeconds);

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[Leaderboard] POST failed: {request.error}");
                    return false;
                }

                return request.responseCode == 201;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Leaderboard] POST exception: {e.Message}");
                return false;
            }
        }

        private List<LeaderboardEntry> ParseLeaderboardJson(string json)
        {
            var entries = new List<LeaderboardEntry>();

            try
            {
                // Unity JSON 파서는 배열을 직접 파싱 못하므로 래퍼 사용
                string wrappedJson = $"{{\"items\":{json}}}";
                var wrapper = JsonUtility.FromJson<LeaderboardListWrapper>(wrappedJson);

                if (wrapper?.items != null)
                {
                    foreach (var item in wrapper.items)
                    {
                        entries.Add(new LeaderboardEntry
                        {
                            PlayerName = item.playerName,
                            Score = item.score,
                            MaxCombo = item.maxCombo,
                            TotalCleared = item.totalCleared,
                            Difficulty = item.difficulty,
                            Rank = item.rank,
                            CreatedAt = DateTime.TryParse(item.createdAt, out var dt) ? dt : DateTime.MinValue
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Leaderboard] JSON parse error: {e.Message}");
            }

            return entries;
        }

        [Serializable]
        private class LeaderboardItem
        {
            public string playerName;
            public int score;
            public int maxCombo;
            public int totalCleared;
            public string difficulty;
            public string createdAt;
            public int rank;
        }

        [Serializable]
        private class LeaderboardListWrapper
        {
            public LeaderboardItem[] items;
        }

        /// <summary>
        /// Dictionary를 JSON으로 직렬화하기 위한 래퍼.
        /// </summary>
        [Serializable]
        private class PostDataWrapper
        {
            public string playerName;
            public int score;
            public int maxCombo;
            public int totalCleared;
            public string difficulty;
            public int gameDuration;

            public PostDataWrapper(Dictionary<string, object> data)
            {
                playerName = data.GetValueOrDefault("playerName", "") as string;
                score = Convert.ToInt32(data.GetValueOrDefault("score", 0));
                maxCombo = Convert.ToInt32(data.GetValueOrDefault("maxCombo", 0));
                totalCleared = Convert.ToInt32(data.GetValueOrDefault("totalCleared", 0));
                difficulty = data.GetValueOrDefault("difficulty", "Easy") as string;
                gameDuration = Convert.ToInt32(data.GetValueOrDefault("gameDuration", 0));
            }
        }
    }
}
