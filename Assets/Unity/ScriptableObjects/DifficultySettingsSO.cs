using System.Collections.Generic;
using BlockPuzzle.Core.Game;
using BlockPuzzle.Core.Interfaces;
using UnityEngine;

namespace BlockPuzzle.Unity.ScriptableObjects
{
    /// <summary>
    /// 난이도별 시각/게임플레이 설정.
    /// Unity Inspector에서 편집 가능.
    /// </summary>
    [CreateAssetMenu(fileName = "DifficultySettings", menuName = "ChainCrush/Difficulty Settings")]
    public class DifficultySettingsSO : ScriptableObject
    {
        [System.Serializable]
        public class DifficultySetting
        {
            public Difficulty Difficulty;
            public Color BlockColorTint;
            public float AnimationSpeed = 1f;
            public Sprite BlockSprite;
        }

        [SerializeField] private DifficultySetting[] _settings;

        private Dictionary<Difficulty, DifficultySetting> _lookup;

        private void OnEnable()
        {
            _lookup = new Dictionary<Difficulty, DifficultySetting>();
            if (_settings != null)
            {
                foreach (var setting in _settings)
                {
                    _lookup[setting.Difficulty] = setting;
                }
            }
        }

        public DifficultySetting GetSetting(Difficulty difficulty)
        {
            if (_lookup != null && _lookup.TryGetValue(difficulty, out var setting))
                return setting;

            return _settings?.Length > 0 ? _settings[0] : null;
        }
    }
}
