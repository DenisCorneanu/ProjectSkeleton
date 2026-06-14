namespace TheAdventure;

public enum RevealResult
{
    None,
    Safe,
    Mine
}

public readonly record struct Position(int Row, int Column);

public sealed class Cell
{
    public bool HasMine { get; set; }
    public bool IsRevealed { get; set; }
    public bool IsFlagged { get; set; }
    public int NeighborMines { get; set; }
}

public sealed class Board
{
    private readonly Cell[,] _cells;
    private readonly Random _random = new();
    private readonly int _mineCount;

    public int Rows { get; }
    public int Columns { get; }

    public Board(int rows, int columns, int mineCount)
    {
        Rows = rows;
        Columns = columns;
        _mineCount = mineCount;
        _cells = new Cell[Rows, Columns];

        for (var row = 0; row < Rows; row++)
        {
            for (var column = 0; column < Columns; column++)
            {
                _cells[row, column] = new Cell();
            }
        }

        PlaceMines();
        CalculateNeighborMines();
    }

    public int FlagCount
    {
        get
        {
            var count = 0;

            for (var row = 0; row < Rows; row++)
            {
                for (var column = 0; column < Columns; column++)
                {
                    if (_cells[row, column].IsFlagged)
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }

    public int Score
    {
        get
        {
            var score = 0;

            for (var row = 0; row < Rows; row++)
            {
                for (var column = 0; column < Columns; column++)
                {
                    var cell = _cells[row, column];

                    if (cell.IsRevealed && !cell.HasMine)
                    {
                        score += 10;
                    }
                }
            }

            return score;
        }
    }

    public bool AllSafeCellsRevealed
    {
        get
        {
            for (var row = 0; row < Rows; row++)
            {
                for (var column = 0; column < Columns; column++)
                {
                    var cell = _cells[row, column];

                    if (!cell.HasMine && !cell.IsRevealed)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    public Cell GetCell(Position position)
    {
        return _cells[position.Row, position.Column];
    }

    public void ToggleFlag(Position position)
    {
        if (!IsInsideBoard(position))
        {
            return;
        }

        var cell = GetCell(position);

        if (cell.IsRevealed)
        {
            return;
        }

        cell.IsFlagged = !cell.IsFlagged;
    }

    public RevealResult Reveal(Position position)
    {
        if (!IsInsideBoard(position))
        {
            return RevealResult.None;
        }

        var cell = GetCell(position);

        if (cell.IsFlagged || cell.IsRevealed)
        {
            return RevealResult.None;
        }

        cell.IsRevealed = true;

        if (cell.HasMine)
        {
            return RevealResult.Mine;
        }

        if (cell.NeighborMines == 0)
        {
            RevealEmptyArea(position);
        }

        return RevealResult.Safe;
    }

    public void RevealAllMines()
    {
        for (var row = 0; row < Rows; row++)
        {
            for (var column = 0; column < Columns; column++)
            {
                var cell = _cells[row, column];

                if (cell.HasMine)
                {
                    cell.IsRevealed = true;
                }
            }
        }
    }

    private void PlaceMines()
    {
        var placedMines = 0;

        while (placedMines < _mineCount)
        {
            var row = _random.Next(Rows);
            var column = _random.Next(Columns);
            var cell = _cells[row, column];

            if (cell.HasMine)
            {
                continue;
            }

            cell.HasMine = true;
            placedMines++;
        }
    }

    private void CalculateNeighborMines()
    {
        for (var row = 0; row < Rows; row++)
        {
            for (var column = 0; column < Columns; column++)
            {
                var position = new Position(row, column);
                var count = 0;

                foreach (var neighbor in GetNeighbors(position))
                {
                    if (GetCell(neighbor).HasMine)
                    {
                        count++;
                    }
                }

                _cells[row, column].NeighborMines = count;
            }
        }
    }

    private void RevealEmptyArea(Position start)
    {
        var positionsToCheck = new Queue<Position>();
        positionsToCheck.Enqueue(start);

        while (positionsToCheck.Count > 0)
        {
            var current = positionsToCheck.Dequeue();

            foreach (var neighbor in GetNeighbors(current))
            {
                var cell = GetCell(neighbor);

                if (cell.IsRevealed || cell.IsFlagged || cell.HasMine)
                {
                    continue;
                }

                cell.IsRevealed = true;

                if (cell.NeighborMines == 0)
                {
                    positionsToCheck.Enqueue(neighbor);
                }
            }
        }
    }

    private IEnumerable<Position> GetNeighbors(Position position)
    {
        for (var rowOffset = -1; rowOffset <= 1; rowOffset++)
        {
            for (var columnOffset = -1; columnOffset <= 1; columnOffset++)
            {
                if (rowOffset == 0 && columnOffset == 0)
                {
                    continue;
                }

                var neighbor = new Position(position.Row + rowOffset, position.Column + columnOffset);

                if (IsInsideBoard(neighbor))
                {
                    yield return neighbor;
                }
            }
        }
    }

    private bool IsInsideBoard(Position position)
    {
        return position.Row >= 0
            && position.Row < Rows
            && position.Column >= 0
            && position.Column < Columns;
    }
}