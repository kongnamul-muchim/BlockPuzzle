using BlockPuzzle.Core.Interfaces;

namespace BlockPuzzle.Core.Game
{
    public class Block : IBlock
    {
        public BlockColor Color { get; private set; }
        public int Row { get; private set; }
        public int Column { get; private set; }
        public BlockState State { get; set; }

        public Block(BlockColor color, int row, int column)
        {
            Color = color;
            Row = row;
            Column = column;
            State = BlockState.Normal;
        }

        public void MoveTo(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public IBlock Clone()
        {
            return new Block(Color, Row, Column)
            {
                State = State
            };
        }

        public override string ToString()
        {
            return $"Block({Color} @ [{Row},{Column}] {State})";
        }
    }
}
