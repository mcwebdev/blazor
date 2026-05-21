namespace FlowBoard.Web.Services;

public sealed class AppActionDispatcher
{
    private readonly List<Func<Task>> _newTaskHandlers = [];
    private readonly List<Func<Task>> _boardReplayHandlers = [];

    public event Action? StateChanged;

    public bool CanRequestNewTask => _newTaskHandlers.Count > 0;
    public bool CanRequestBoardReplay => _boardReplayHandlers.Count > 0;

    public IDisposable RegisterNewTaskHandler(Func<Task> handler)
    {
        return RegisterHandler(_newTaskHandlers, handler);
    }

    public IDisposable RegisterBoardReplayHandler(Func<Task> handler)
    {
        return RegisterHandler(_boardReplayHandlers, handler);
    }

    public async Task RequestNewTaskAsync()
    {
        foreach (var handler in _newTaskHandlers.ToArray())
        {
            await handler();
        }
    }

    public async Task RequestBoardReplayAsync()
    {
        foreach (var handler in _boardReplayHandlers.ToArray())
        {
            await handler();
        }
    }

    private IDisposable RegisterHandler(ICollection<Func<Task>> handlers, Func<Task> handler)
    {
        handlers.Add(handler);
        StateChanged?.Invoke();

        return new Subscription(() =>
        {
            if (handlers.Remove(handler))
            {
                StateChanged?.Invoke();
            }
        });
    }

    private sealed class Subscription(Action dispose) : IDisposable
    {
        private bool _isDisposed;

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            dispose();
        }
    }
}
