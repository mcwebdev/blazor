namespace FlowBoard.Web.Services;

public sealed class AppActionDispatcher
{
    public event Func<Task>? NewTaskRequested;

    public async Task RequestNewTaskAsync()
    {
        var handlers = NewTaskRequested?.GetInvocationList()
            .Cast<Func<Task>>()
            .ToArray();

        if (handlers is null || handlers.Length == 0)
            return;

        foreach (var handler in handlers)
        {
            await handler();
        }
    }
}
