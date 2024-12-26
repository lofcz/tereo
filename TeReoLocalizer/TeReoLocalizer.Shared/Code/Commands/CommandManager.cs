using Microsoft.JSInterop;
using TeReoLocalizer.Shared.Components.Pages;

namespace TeReoLocalizer.Shared.Code.Commands;

public class CommandManager : IDisposable
{
    public Func<Task>? OnBeforeJump { get; set; }
    public Func<Task>? OnAfterJump { get; set; }
    public Func<RewindActions, ICommand, Task>? OnBeforeRewindProgressCommand { get; set; }
    public Func<RewindActions, ICommand, Task>? OnAfterRewindProgressCommand { get; set; }

    private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
    private readonly CircularBuffer<ICommand> undoBuffer;
    private readonly CircularBuffer<ICommand> redoBuffer;
    
    private class CircularBuffer<T>
    {
        private readonly T[] buffer;
        private int head;

        public CircularBuffer(int capacity)
        {
            buffer = new T[capacity];
            head = 0;
            Count = 0;
        }

        public void Push(T item)
        {
            buffer[head] = item;
            head = (head + 1) % buffer.Length;
            if (Count < buffer.Length)
            {
                Count++;
            }
        }

        public T Pop()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("Buffer is empty");
            }
        
            head = (head - 1 + buffer.Length) % buffer.Length;
            Count--;
            return buffer[head];
        }

        public IEnumerable<T> GetItems()
        {
            if (Count is 0)
            {
                yield break;
            }
            
            int start = (head - Count + buffer.Length) % buffer.Length;
            
            for (int i = 0; i < Count; i++)
            {
                yield return buffer[(start + i) % buffer.Length];
            }
        }


        public int Count { get; private set; }

        public void Clear() 
        {
            Array.Clear(buffer, 0, buffer.Length);
            head = 0;
            Count = 0;
        }
    }
    
    public CommandManager(int historySize = 1000)
    {
        undoBuffer = new CircularBuffer<ICommand>(historySize);
        redoBuffer = new CircularBuffer<ICommand>(historySize);
    }
    
    public async Task<DataOrException<bool>> Execute(ICommand command)
    {
        await semaphore.WaitAsync();
        try
        {
            DataOrException<bool> executed = await command.Do(true);

            if (executed.Exception is null && executed.Data)
            {
                undoBuffer.Push(command);
                redoBuffer.Clear();
            }

            return executed;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task Undo()
    {
        await semaphore.WaitAsync();
        try
        {
            await UndoInternal();
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task UndoInternal()
    {
        if (undoBuffer.Count is 0)
        {
            return;
        }

        ICommand command = undoBuffer.Pop();

        if (command.Progress is not null)
        {
            if (OnBeforeRewindProgressCommand is not null)
            {
                await OnBeforeRewindProgressCommand.Invoke(RewindActions.Undo, command);
            }
        }
        
        await command.Undo();
        
        if (command.Progress is not null)
        {
            if (OnAfterRewindProgressCommand is not null)
            {
                await OnAfterRewindProgressCommand.Invoke(RewindActions.Undo, command);
            }
        }
        
        redoBuffer.Push(command);
    }

    public async Task Redo()
    {
        await semaphore.WaitAsync();
        try
        {
            await RedoInternal();
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task RedoInternal()
    {
        if (redoBuffer.Count is 0)
        {
            return;
        }

        ICommand command = redoBuffer.Pop();
        
        if (command.Progress is not null)
        {
            if (OnBeforeRewindProgressCommand is not null)
            {
                await OnBeforeRewindProgressCommand.Invoke(RewindActions.Redo, command);
            }
        }
        
        await command.Do(false);
        
        if (command.Progress is not null)
        {
            if (OnAfterRewindProgressCommand is not null)
            {
                await OnAfterRewindProgressCommand.Invoke(RewindActions.Redo, command);
            }
        }
        
        undoBuffer.Push(command);
    }

    public bool CanUndo => undoBuffer.Count > 0;
    public bool CanRedo => redoBuffer.Count > 0;
    public bool AnyHistory => CanUndo || CanRedo;
    
    public CommandHistory GetHistory(int limit = 100)
    {
        if (undoBuffer.Count + redoBuffer.Count <= limit)
        {
            return new CommandHistory(
                undoBuffer.GetItems().Select((cmd, index) => new HistoryItem(cmd,  -(undoBuffer.Count - index))).ToList(),
                redoBuffer.GetItems().Select((cmd, index) => new HistoryItem(cmd, index + 1)).ToList()
            );
        }
    
        double undoRatio = (double)undoBuffer.Count / (undoBuffer.Count + redoBuffer.Count);
        int undoLimit = (int)(limit * undoRatio);
        int redoLimit = limit - undoLimit;
    
        if (undoLimit is 0 && undoBuffer.Count > 0) 
        {
            undoLimit = 1;
            redoLimit = limit - 1;
        }
    
        if (redoLimit is 0 && redoBuffer.Count > 0)
        {
            redoLimit = 1;
            undoLimit = limit - 1;
        }

        List<HistoryItem> before = undoBuffer.GetItems()
            .Select((cmd, index) => new HistoryItem(cmd, -(undoBuffer.Count - index)))
            .Take(undoLimit)
            .ToList();

        List<HistoryItem> after = redoBuffer.GetItems()
            .Select((cmd, index) => new HistoryItem(cmd, index + 1))
            .Take(redoLimit)
            .ToList();

        return new CommandHistory(before, after);
    }

    /// <summary>
    /// Moves to target point in history.
    /// </summary>
    /// <param name="item"></param>
    public async Task Jump(HistoryItem item)
    {
        await Jump(item.Index);
    }

    /// <summary>
    /// Moves to target point in history.
    /// </summary>
    /// <param name="relativeSteps"></param>
    public async Task Jump(int relativeSteps)
    {
        await semaphore.WaitAsync();
        try
        {
            if (OnBeforeJump is not null)
            {
                await OnBeforeJump.Invoke();
            }
            
            switch (relativeSteps)
            {
                case < 0:
                {
                    int stepsBack = Math.Abs(relativeSteps);
                    for (int i = 0; i < stepsBack; i++)
                    {
                        await UndoInternal();
                    }

                    break;
                }
                case > 0:
                {
                    for (int i = 0; i < relativeSteps; i++)
                    {
                        await RedoInternal();
                    }

                    break;
                }
            }
            
            if (OnAfterJump is not null)
            {
                await OnAfterJump.Invoke();
            }
        }
        finally
        {
            semaphore.Release();
        }
    }
    
    public void Dispose()
    {
        semaphore.Dispose();
        GC.SuppressFinalize(this);
    }
}

