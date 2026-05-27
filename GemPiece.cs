namespace ErenshorGems
{
    public class GemPiece
    {
        public GemType Type { get; set; }
        public int Column { get; set; }
        public float FractionalRow { get; set; }

        public int Row => (int)FractionalRow;

        public GemPiece(GemType type, int startColumn)
        {
            Type = type;
            Column = startColumn;
            FractionalRow = 0f;
        }

        public void MoveLeft()
        {
            if (Column > 0)
                Column--;
        }

        public void MoveRight()
        {
            if (Column < GemsBoard.Columns - 1)
                Column++;
        }

        public bool Advance(float amount)
        {
            FractionalRow += amount;
            return FractionalRow >= GemsBoard.Rows;
        }

        public bool WouldCollide(GemsBoard board)
        {
            int nextRow = Row + 1;
            if (nextRow >= GemsBoard.Rows)
                return true;
            return board.IsCellOccupied(Column, nextRow);
        }

        public bool CanMoveTo(GemsBoard board, int newCol)
        {
            if (newCol < 0 || newCol >= GemsBoard.Columns)
                return false;
            return !board.IsCellOccupied(newCol, Row);
        }
    }
}
