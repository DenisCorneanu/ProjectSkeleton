using Silk.NET.SDL;

namespace TheAdventure;

public sealed unsafe class GameRenderer
{
    private const int CellSize = 42;
    private const int CellGap = 4;
    private const int BoardLeft = 60;
    private const int BoardTop = 60;

    private readonly Sdl _sdl;
    private readonly Renderer* _renderer;

    public GameRenderer(Sdl sdl, IntPtr renderer)
    {
        _sdl = sdl;
        _renderer = (Renderer*)renderer;
    }

    public void Render(Board board, GameStatus status)
    {
        _sdl.SetRenderDrawColor(_renderer, 45, 45, 45, 255);
        _sdl.RenderClear(_renderer);

        DrawBoard(board, status);

        _sdl.RenderPresent(_renderer);
    }

    public bool TryGetBoardPosition(Board board, int mouseX, int mouseY, out Position position)
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

        if (row < 0 || row >= board.Rows || column < 0 || column >= board.Columns)
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

    private void DrawBoard(Board board, GameStatus status)
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
                    FillRect(x, y, CellSize, CellSize, 55, 55, 55);
                    DrawRect(x, y, CellSize, CellSize, 50, 50, 50);

                    if (cell.HasMine)
                    {
                        DrawMine(x, y, status == GameStatus.Lost);
                    }
                    else if (cell.NeighborMines > 0)
                    {
                        DrawDigit(cell.NeighborMines, x, y);
                    }
                }
                else
                {
                    FillRect(x, y, CellSize, CellSize, 75, 75, 75);
                    DrawRect(x, y, CellSize, CellSize, 105, 105, 105);

                    if (cell.IsFlagged)
                    {
                        DrawFlag(x, y);
                    }
                }
            }
        }
    }

    private void FillRect(int x, int y, int width, int height, byte r, byte g, byte b)
    {
        _sdl.SetRenderDrawColor(_renderer, r, g, b, 255);

        for (var currentY = y; currentY < y + height; currentY++)
        {
            _sdl.RenderDrawLine(_renderer, x, currentY, x + width - 1, currentY);
        }
    }

    private void DrawRect(int x, int y, int width, int height, byte r, byte g, byte b)
    {
        _sdl.SetRenderDrawColor(_renderer, r, g, b, 255);

        _sdl.RenderDrawLine(_renderer, x, y, x + width - 1, y);
        _sdl.RenderDrawLine(_renderer, x, y + height - 1, x + width - 1, y + height - 1);
        _sdl.RenderDrawLine(_renderer, x, y, x, y + height - 1);
        _sdl.RenderDrawLine(_renderer, x + width - 1, y, x + width - 1, y + height - 1);
    }

    private void DrawFlag(int x, int y)
    {
        var poleX = x + 14;
        var poleTop = y + 10;
        var poleBottom = y + 31;

        _sdl.SetRenderDrawColor(_renderer, 220, 220, 220, 255);
        _sdl.RenderDrawLine(_renderer, poleX, poleTop, poleX, poleBottom);

        FillRect(poleX + 1, poleTop + 2, 16, 10, 180, 40, 40);
        FillRect(poleX - 4, poleBottom, 14, 4, 180, 180, 180);
    }

    private void DrawMine(int x, int y, bool exploded)
    {
        var centerX = x + CellSize / 2;
        var centerY = y + CellSize / 2;

        if (exploded)
        {
            FillRect(x + 4, y + 4, CellSize - 8, CellSize - 8, 130, 0, 0);
        }

        FillRect(centerX - 8, centerY - 8, 16, 16, 10, 10, 10);

        _sdl.SetRenderDrawColor(_renderer, 10, 10, 10, 255);
        _sdl.RenderDrawLine(_renderer, centerX - 13, centerY, centerX + 13, centerY);
        _sdl.RenderDrawLine(_renderer, centerX, centerY - 13, centerX, centerY + 13);
        _sdl.RenderDrawLine(_renderer, centerX - 10, centerY - 10, centerX + 10, centerY + 10);
        _sdl.RenderDrawLine(_renderer, centerX + 10, centerY - 10, centerX - 10, centerY + 10);
    }

    private void DrawDigit(int digit, int x, int y)
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