using System.Collections.Generic;
using BlockPuzzle.Core.Interfaces;

namespace BlockPuzzle.Core.Game
{
    public class DifficultyConfig : IDifficultyConfig
    {
        private static readonly Dictionary<Difficulty, BlockColor[]> ColorPools = new()
        {
            [Difficulty.Easy]   = new[] { BlockColor.Red, BlockColor.Yellow, BlockColor.Green, BlockColor.Blue },
            [Difficulty.Normal] = new[] { BlockColor.Red, BlockColor.Yellow, BlockColor.Green, BlockColor.Blue, BlockColor.Purple },
            [Difficulty.Hard]   = new[] { BlockColor.Red, BlockColor.Orange, BlockColor.Yellow, BlockColor.Green, BlockColor.Blue, BlockColor.Purple }
        };

        public Difficulty CurrentDifficulty { get; set; } = Difficulty.Easy;

        public int ColorCount => ColorPools[CurrentDifficulty].Length;

        public IReadOnlyList<BlockColor> GetColorPool()
        {
            return ColorPools[CurrentDifficulty];
        }
    }
}
