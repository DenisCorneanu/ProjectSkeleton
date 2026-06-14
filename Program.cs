using System;
using System.Collections.Generic;
using Silk.NET.SDL;

namespace TheAdventure;

public static class Program
{
    private const int WindowWidth = 600;
    private const int WindowHeight = 600;

    private const int Rows = 10;
    private const int Columns = 10;
    private const int MineCount = 15;

    private const int CellSize = 42;
    private const int CellGap = 4;
    private const int BoardLeft = 60;
    private const int BoardTop = 60;

    public static void Main()
    {
        var sdl = new Sdl(new SdlContext());

        var initResult = sdl.Init(Sdl.InitVideo | Sdl.InitEvents | Sdl.InitTimer);
        if (initResult < 0)
        {
            throw new InvalidOperationException("Failed to initialize SDL.");
        }

        IntPtr window;
        unsafe
        {
            window = (IntPtr)sdl.CreateWindow(
                "Minefield Explorer",
                Sdl.WindowposUndefined,
                Sdl.WindowposUndefined,
                WindowWidth,
                WindowHeight,
                (uint)WindowFlags.Resizable | (uint)WindowFlags.AllowHighdpi);

            if (window == IntPtr.Zero)
            {
                var ex = sdl.GetErrorAsException();
                if (ex != null)
                {
                    throw ex;
                }

                throw new Exception("Failed to create window.");
            }
        }

        IntPtr renderer;
        unsafe
        {
            renderer = (IntPtr)sdl.CreateRenderer((Window*)window, -1, (uint)RendererFlags.Accelerated);
            sdl.RenderSetVSync((Renderer*)renderer, 1);
        }

        if (renderer == IntPtr.Zero)
        {
            var ex = sdl.GetErrorAsException();
            if (ex != null)
            {
                throw ex;
            }

            throw new Exception("Failed to create renderer.");
        }

        var board = new Board(Rows, Columns, MineCount);
        var status = GameStatus.Running;
        var quit = false;
        var ev = new Event();

        UpdateWindowTitle(sdl, window, board, status);

        while (!quit)
        {
            while (sdl.PollEvent(ref ev) != 0)
            {
                if (ev.Type == (uint)EventType.Quit)
                {
                    quit = true;
                    break;
                }

                switch (ev.Type)
                {
                    case (uint)EventType.Mousebuttondown:
                    {
                        if (status != GameStatus.Running)
                        {
                            break;
                        }

                        if (!TryGetBoardPosition(ev.Button.X, ev.Button.Y, out var position))
                        {
                            break;
                        }

                        if (ev.Button.Button == (byte)MouseButton.Primary)
                        {
                            var result = board.Reveal(position);

                            if (result == RevealResult.Mine)
                            {
                                status = GameStatus.Lost;
                                board.RevealAllMines();
                            }
                            else if (board.AllSafeCellsRevealed)
                            {
                                status = GameStatus.Won;
                                board.RevealAllMines();
                            }
                        }
                        else if (ev.Button.Button == (byte)MouseButton.Secondary)
                        {
                            board.ToggleFlag(position);
                        }

                        UpdateWindowTitle(sdl, window, board, status);
                        break;
                    }

                    case (uint)EventType.Keydown:
                    {
                        var key = (KeyCode)ev.Key.Keysym.Scancode;

                        if (key == KeyCode.Q || key == KeyCode.Escape)
                        {
                            quit = true;
                        }
                        else if (key == KeyCode.R)
                        {
                            board = new Board(Rows, Columns, MineCount);
                            status = GameStatus.Running;
                            UpdateWindowTitle(sdl, window, board, status);
                        }

                        break;
                    }
                }
            }

            unsafe
            {
                var currentRenderer = (Renderer*)renderer;

                sdl.SetRenderDrawColor(currentRenderer, 45, 45, 45, 255);
                sdl.RenderClear(currentRenderer);

                DrawBoard(sdl, currentRenderer, board, status);

                sdl.RenderPresent(currentRenderer);
            }

            sdl.Delay(16);
        }

        unsafe
        {
            sdl.DestroyRenderer((Renderer*)renderer);
            sdl.DestroyWindow((Window*)window);
        }

        sdl.Quit();
    }

    private static unsafe void UpdateWindowTitle(Sdl sdl, IntPtr window, Board board, GameStatus status)
    {
        var message = status switch
        {
            GameStatus.Running => $"Minefield Explorer | Score: {board.Score} | Flags: {board.FlagCount}/{MineCount} | R: restart | Q: quit",
            GameStatus.Won => $"You won! Score: {board.Score} | Press R to restart or Q to quit",
            GameStatus.Lost => $"You lost! Score: {board.Score} | Press R to restart or Q to quit",
            _ => "Minefield Explorer"
        };

        sdl.SetWindowTitle((Window*)window, message);
    }

    private static bool TryGetBoardPosition(int mouseX, int mouseY, out Position position)
    {
        position = new Position(0, 0);

        var localX = mouseX - BoardLeft;
        var localY = mouseY - BoardTop;

        if (localX < 0 || localY < 0)
        {
            return false;
        }

        var pitch = CellSize + CellGap;
        var column = localX / pitch;
        var row = localY / pitch;

        if (row < 0 || row >= Rows || column < 0 || column >= Columns)
        {
            return false;
        }

        if (localX % pitch >= CellSize || localY % pitch >= CellSize)
        {
            return false;
        }

        position = new Position(row, column);
        return true;
    }

    private static unsafe void DrawBoard(Sdl sdl, Renderer* renderer, Board board, GameStatus status)
    {
        for (var row = 0; row < board.Rows; row++)
        {
            for (var column = 0; column < board.Columns; column++)
            {
                var position = new Position(row, column);
                var cell = board.GetCell(position);

                var x = BoardLeft + column * (CellSize + CellGap);
                var y = BoardTop + row * (CellSize + CellGap);

                if (cell.IsRevealed)
                {
                    FillRect(sdl, renderer, x, y, CellSize, CellSize, 55, 55, 55);
                    DrawRect(sdl, renderer, x, y, CellSize, CellSize, 50, 50, 50);

                    if (cell.HasMine)
                    {
                        DrawMine(sdl, renderer, x, y, status == GameStatus.Lost);
                    }
                    else if (cell.NeighborMines > 0)
                    {
                        DrawDigit(sdl, renderer, cell.NeighborMines, x, y);
                    }
                }
                else
                {
                    FillRect(sdl, renderer, x, y, CellSize, CellSize, 75, 75, 75);
                    DrawRect(sdl, renderer, x, y, CellSize, CellSize, 105, 105, 105);

                    if (cell.IsFlagged)
                    {
                        DrawFlag(sdl, renderer, x, y);
                    }
                }
            }
        }
    }

    private static unsafe void FillRect(Sdl sdl, Renderer* renderer, int x, int y, int width, int height, byte r, byte g, byte b)
    {
        sdl.SetRenderDrawColor(renderer, r, g, b, 255);

        for (var currentY = y; currentY < y + height; currentY++)
        {
            sdl.RenderDrawLine(renderer, x, currentY, x + width - 1, currentY);
        }
    }

    private static unsafe void DrawRect(Sdl sdl, Renderer* renderer, int x, int y, int width, int height, byte r, byte g, byte b)
    {
        sdl.SetRenderDrawColor(renderer, r, g, b, 255);

        sdl.RenderDrawLine(renderer, x, y, x + width - 1, y);
        sdl.RenderDrawLine(renderer, x, y + height - 1, x + width - 1, y + height - 1);
        sdl.RenderDrawLine(renderer, x, y, x, y + height - 1);
        sdl.RenderDrawLine(renderer, x + width - 1, y, x + width - 1, y + height - 1);
    }

    private static unsafe void DrawFlag(Sdl sdl, Renderer* renderer, int x, int y)
    {
        var poleX = x + 14;
        var poleTop = y + 10;
        var poleBottom = y + 31;

        sdl.SetRenderDrawColor(renderer, 220, 220, 220, 255);
        sdl.RenderDrawLine(renderer, poleX, poleTop, poleX, poleBottom);

        FillRect(sdl, renderer, poleX + 1, poleTop + 2, 16, 10, 180, 40, 40);
        FillRect(sdl, renderer, poleX - 4, poleBottom, 14, 4, 180, 180, 180);
    }

    private static unsafe void DrawMine(Sdl sdl, Renderer* renderer, int x, int y, bool exploded)
    {
        var centerX = x + CellSize / 2;
        var centerY = y + CellSize / 2;

        if (exploded)
        {
            FillRect(sdl, renderer, x + 4, y + 4, CellSize - 8, CellSize - 8, 130, 0, 0);
        }

        FillRect(sdl, renderer, centerX - 8, centerY - 8, 16, 16, 10, 10, 10);

        sdl.SetRenderDrawColor(renderer, 10, 10, 10, 255);
        sdl.RenderDrawLine(renderer, centerX - 13, centerY, centerX + 13, centerY);
        sdl.RenderDrawLine(renderer, centerX, centerY - 13, centerX, centerY + 13);
        sdl.RenderDrawLine(renderer, centerX - 10, centerY - 10, centerX + 10, centerY + 10);
        sdl.RenderDrawLine(renderer, centerX + 10, centerY - 10, centerX - 10, centerY + 10);
    }

    private static unsafe void DrawDigit(Sdl sdl, Renderer* renderer, int digit, int x, int y)
    {
        var pattern = GetDigitPattern(digit);
        var color = GetDigitColor(digit);
        var blockSize = 5;
        var startX = x + 13;
        var startY = y + 8;

        for (var row = 0; row < pattern.Length; row++)
        {
            for (var column = 0; column < pattern[row].Length; column++)
            {
                if (pattern[row][column] == '1')
                {
                    FillRect(
                        sdl,
                        renderer,
                        startX + column * blockSize,
                        startY + row * blockSize,
                        blockSize - 1,
                        blockSize - 1,
                        color.R,
                        color.G,
                        color.B);
                }
            }
        }
    }

    private static string[] GetDigitPattern(int digit)
    {
        return digit switch
        {
            1 => new[] { "010", "110", "010", "010", "111" },
            2 => new[] { "111", "001", "111", "100", "111" },
            3 => new[] { "111", "001", "111", "001", "111" },
            4 => new[] { "101", "101", "111", "001", "001" },
            5 => new[] { "111", "100", "111", "001", "111" },
            6 => new[] { "111", "100", "111", "101", "111" },
            7 => new[] { "111", "001", "010", "010", "010" },
            8 => new[] { "111", "101", "111", "101", "111" },
            _ => new[] { "000", "000", "000", "000", "000" }
        };
    }

    private static (byte R, byte G, byte B) GetDigitColor(int digit)
    {
        return digit switch
        {
            1 => (80, 100, 230),
            2 => (50, 160, 70),
            3 => (190, 45, 45),
            4 => (90, 90, 200),
            5 => (160, 70, 70),
            6 => (40, 150, 150),
            7 => (180, 180, 180),
            8 => (230, 150, 50),
            _ => (255, 255, 255)
        };
    }
}

public enum GameStatus
{
    Running,
    Won,
    Lost
}

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