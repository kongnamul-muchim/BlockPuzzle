using System.Collections.Generic;
using BlockPuzzle.Core.Game;

namespace BlockPuzzle.Core.Interfaces
{
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard
    }

    public interface IDifficultyConfig
    {
        Difficulty CurrentDifficulty { get; set; }
        IReadOnlyList<BlockColor> GetColorPool();
        int ColorCount { get; }
    }
}
