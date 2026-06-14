using Silk.NET.SDL;

namespace TheAdventure;

public static class Program
{
    private const int WindowWidth = 600;
    private const int WindowHeight = 600;

    private const int Rows = 10;
    private const int Columns = 10;
    private const int MineCount = 15;

    public static async Task Main()
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

        var gameRenderer = new GameRenderer(sdl, renderer);
        var board = new Board(Rows, Columns, MineCount);
        var status = GameStatus.Running;
        var quit = false;
        var ev = new Event();

        var highScoreService = new HighScoreService();
        var bestScore = await highScoreService.GetBestScoreAsync();

        UpdateWindowTitle(sdl, window, board, status, bestScore);

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

                        if (!gameRenderer.TryGetBoardPosition(board, ev.Button.X, ev.Button.Y, out var position))
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

                                await highScoreService.SaveScoreAsync(board.Score);
                                bestScore = await highScoreService.GetBestScoreAsync();
                            }
                        }
                        else if (ev.Button.Button == (byte)MouseButton.Secondary)
                        {
                            board.ToggleFlag(position);
                        }

                        UpdateWindowTitle(sdl, window, board, status, bestScore);
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
                            bestScore = await highScoreService.GetBestScoreAsync();

                            UpdateWindowTitle(sdl, window, board, status, bestScore);
                        }

                        break;
                    }
                }
            }

            gameRenderer.Render(board, status);

            sdl.Delay(16);
        }

        unsafe
        {
            sdl.DestroyRenderer((Renderer*)renderer);
            sdl.DestroyWindow((Window*)window);
        }

        sdl.Quit();
    }

    private static unsafe void UpdateWindowTitle(Sdl sdl, IntPtr window, Board board, GameStatus status, int bestScore)
    {
        var message = status switch
        {
            GameStatus.Running => $"Minefield Explorer | Score: {board.Score} | Best: {bestScore} | Flags: {board.FlagCount}/{MineCount} | R: restart | Q: quit",
            GameStatus.Won => $"You won! Score: {board.Score} | Best: {bestScore} | Press R to restart or Q to quit",
            GameStatus.Lost => $"You lost! Score: {board.Score} | Best: {bestScore} | Press R to restart or Q to quit",
            _ => "Minefield Explorer"
        };

        sdl.SetWindowTitle((Window*)window, message);
    }
}

public enum GameStatus
{
    Running,
    Won,
    Lost
}