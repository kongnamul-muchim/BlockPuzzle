using System;
using System.Collections.Generic;
using BlockPuzzle.Core.Interfaces;

namespace BlockPuzzle.Core.Game
{
    public class ChainDetector : IChainDetector
    {
        private readonly IGrid _grid;

        // 상, 하, 좌, 우
        private static readonly (int dr, int dc)[] Directions =
        {
            (-1, 0), (1, 0), (0, -1), (0, 1)
        };

        public ChainDetector(IGrid grid)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        }

        public IReadOnlyList<IBlock> FindConnectedBlocks(int row, int column)
        {
            IBlock startBlock = _grid.GetBlockAt(row, column);
            if (startBlock == null)
                return Array.Empty<IBlock>();

            var visited = new HashSet<(int, int)>();
            var result = new List<IBlock>();
            var queue = new Queue<(int, int)>();

            BlockColor targetColor = startBlock.Color;

            visited.Add((row, column));
            queue.Enqueue((row, column));

            while (queue.Count > 0)
            {
                var (r, c) = queue.Dequeue();
                IBlock current = _grid.GetBlockAt(r, c);

                if (current == null || current.State == BlockState.Removed)
                    continue;

                if (current.Color != targetColor)
                    continue;

                result.Add(current);

                foreach (var (dr, dc) in Directions)
                {
                    int nr = r + dr;
                    int nc = c + dc;

                    if (nr < 0 || nr >= _grid.Rows || nc < 0 || nc >= _grid.Columns)
                        continue;

                    if (visited.Contains((nr, nc)))
                        continue;

                    visited.Add((nr, nc));
                    queue.Enqueue((nr, nc));
                }
            }

            return result;
        }

        public bool CanRemove(IReadOnlyList<IBlock> group)
        {
            return group != null && group.Count >= 2;
        }
    }
}
