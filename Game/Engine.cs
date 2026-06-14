using Silk.NET.SDL;

namespace TheAdventure;

public sealed class Engine : IDisposable
{
    private const int WindowWidth = 600;
    private const int WindowHeight = 600;

    private const int Rows = 10;
    private const int Columns = 10;
    private const int MineCount = 15;

    private readonly Sdl _sdl;
    private readonly IntPtr _window;
    private readonly IntPtr _renderer;
    private readonly GameRenderer _gameRenderer;
    private readonly HighScoreService _highScoreService = new();

    private Board _board = new(Rows, Columns, MineCount);
    private GameStatus _status = GameStatus.Running;
    private bool _quit;
    private int _bestScore;

    public Engine()
    {
        _sdl = new Sdl(new SdlContext());

        var initResult = _sdl.Init(Sdl.InitVideo | Sdl.InitEvents | Sdl.InitTimer);
        if (initResult < 0)
        {
            throw new InvalidOperationException("Failed to initialize SDL.");
        }

        unsafe
        {
            _window = (IntPtr)_sdl.CreateWindow(
                "Minefield Explorer",
                Sdl.WindowposUndefined,
                Sdl.WindowposUndefined,
                WindowWidth,
                WindowHeight,
                (uint)WindowFlags.Resizable | (uint)WindowFlags.AllowHighdpi);
        }

        if (_window == IntPtr.Zero)
        {
            var ex = _sdl.GetErrorAsException();
            if (ex != null)
            {
                throw ex;
            }

            throw new Exception("Failed to create window.");
        }

        unsafe
        {
            _renderer = (IntPtr)_sdl.CreateRenderer((Window*)_window, -1, (uint)RendererFlags.Accelerated);
            _sdl.RenderSetVSync((Renderer*)_renderer, 1);
        }

        if (_renderer == IntPtr.Zero)
        {
            var ex = _sdl.GetErrorAsException();
            if (ex != null)
            {
                throw ex;
            }

            throw new Exception("Failed to create renderer.");
        }

        _gameRenderer = new GameRenderer(_sdl, _renderer);
    }

    public async Task RunAsync()
    {
        _bestScore = await _highScoreService.GetBestScoreAsync();
        UpdateWindowTitle();

        var ev = new Event();

        while (!_quit)
        {
            while (_sdl.PollEvent(ref ev) != 0)
            {
                await ProcessEventAsync(ev);
            }

            _gameRenderer.Render(_board, _status);
            _sdl.Delay(16);
        }
    }

    public void Dispose()
    {
        unsafe
        {
            _sdl.DestroyRenderer((Renderer*)_renderer);
            _sdl.DestroyWindow((Window*)_window);
        }

        _sdl.Quit();
    }

    private async Task ProcessEventAsync(Event ev)
    {
        if (ev.Type == (uint)EventType.Quit)
        {
            _quit = true;
            return;
        }

        switch (ev.Type)
        {
            case (uint)EventType.Mousebuttondown:
                await ProcessMouseClickAsync(ev);
                break;

            case (uint)EventType.Keydown:
                await ProcessKeyPressAsync(ev);
                break;
        }
    }

    private async Task ProcessMouseClickAsync(Event ev)
    {
        if (_gameRenderer.IsRestartButtonClicked(ev.Button.X, ev.Button.Y))
        {
            await RestartGameAsync();
            return;
        }

        if (_status != GameStatus.Running)
        {
            return;
        }

        if (!_gameRenderer.TryGetBoardPosition(_board, ev.Button.X, ev.Button.Y, out var position))
        {
            return;
        }

        if (ev.Button.Button == (byte)MouseButton.Primary)
        {
            await RevealCellAsync(position);
        }
        else if (ev.Button.Button == (byte)MouseButton.Secondary)
        {
            _board.ToggleFlag(position);
        }

        UpdateWindowTitle();
    }

    private async Task RevealCellAsync(Position position)
    {
        var result = _board.Reveal(position);

        if (result == RevealResult.Mine)
        {
            _status = GameStatus.Lost;
            _board.RevealAllMines();
            return;
        }

        if (_board.AllSafeCellsRevealed)
        {
            _status = GameStatus.Won;
            _board.RevealAllMines();

            await _highScoreService.SaveScoreAsync(_board.Score);
            _bestScore = await _highScoreService.GetBestScoreAsync();
        }
    }

    private async Task ProcessKeyPressAsync(Event ev)
    {
        var key = (KeyCode)ev.Key.Keysym.Scancode;

        if (key == KeyCode.Q || key == KeyCode.Escape)
        {
            _quit = true;
        }
        else if (key == KeyCode.R)
        {
            await RestartGameAsync();
        }
    }

    private async Task RestartGameAsync()
    {
        _board = new Board(Rows, Columns, MineCount);
        _status = GameStatus.Running;
        _bestScore = await _highScoreService.GetBestScoreAsync();

        UpdateWindowTitle();
    }

    private void UpdateWindowTitle()
    {
        var message = _status switch
        {
            GameStatus.Running => $"Minefield Explorer | Score: {_board.Score} | Best: {_bestScore} | Flags left: {_board.FlagsRemaining}/{MineCount} | R/click button: restart | Q: quit",
            GameStatus.Won => $"You won! Score: {_board.Score} | Best: {_bestScore} | Press R or click button to restart",
            GameStatus.Lost => $"You lost! Score: {_board.Score} | Best: {_bestScore} | Press R or click button to restart",
            _ => "Minefield Explorer"
        };

        unsafe
        {
            _sdl.SetWindowTitle((Window*)_window, message);
        }
    }
}