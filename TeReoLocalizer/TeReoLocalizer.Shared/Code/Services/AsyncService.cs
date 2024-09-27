
namespace TeReoLocalizer.Shared.Code.Services;

public static class AsyncService
{
    public static void Fire(Func<Task> fn)
    {
        Task.Run(async () =>
        {
            await fn.Invoke();
        });
    }
    
    public static void Fire(Func<CancellationToken, Task> fn, int msDelay, CancellationToken ct)
    {
        Task.Run(async () =>
        {
            await Task.Delay(msDelay, ct);
            await fn.Invoke(ct);
        }, ct);
    }
   
    public static Task Fire(Func<Task> fn, CancellationToken ct)
    {
        return Task.Run(async () =>
        {
            await fn.Invoke();
        }, ct);
    }

    public static async Task WhenAll(params Task?[] tasks)
    {
        await Task.WhenAll(tasks.Where(x => x is not null)!);
    }
    
    public static async Task WhenAll(List<Task?> tasks)
    {
        if (tasks.Count is 0)
        {
            return;
        }
        
        await Task.WhenAll(tasks.Where(x => x is not null)!);
    }
    
    public static async Task WhenAll<T>(List<Task<T>> tasks)
    {
        await Task.WhenAll(tasks);
    }
}