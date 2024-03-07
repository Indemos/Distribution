using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Distribution.ServiceSpace
{
  public class ScheduleService : IDisposable
  {
    protected virtual Thread _process { get; set; }
    protected virtual BlockingCollection<Action> _queue { get; set; }
    protected virtual int _count { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public ScheduleService() : this(1, ThreadPriority.Normal)
    {
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="count"></param>
    /// <param name="priority"></param>
    public ScheduleService(int count, ThreadPriority priority)
    {
      _queue = new();
      _count = count;
      _process = new Thread(() => _queue.GetConsumingEnumerable().ForEach(o => o()))
      {
        IsBackground = true,
        Priority = priority
      };

      _process.Start();
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public virtual void Dispose() => _queue.CompleteAdding();

    /// <summary>
    /// Action processor
    /// </summary>
    /// <param name="action"></param>
    public virtual TaskCompletionSource Send(Action action)
    {
      var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

      Enqueue(() =>
      {
        try
        {
          action();
          completion.TrySetResult();
        }
        catch (Exception e)
        {
          completion.TrySetException(e);
        }
      });

      return completion;
    }

    /// <summary>
    /// Delegate processor
    /// </summary>
    /// <param name="action"></param>
    public virtual TaskCompletionSource<T> Send<T>(Func<T> action)
    {
      var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

      Enqueue(() =>
      {
        try
        {
          completion.TrySetResult(action());
        }
        catch (Exception e)
        {
          completion.TrySetException(e);
        }
      });

      return completion;
    }

    /// <summary>
    /// Delegate processor
    /// </summary>
    /// <param name="action"></param>
    public virtual TaskCompletionSource Send(Func<Task> action)
    {
      var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

      Enqueue(() =>
      {
        try
        {
          action().GetAwaiter().GetResult();
          completion.TrySetResult();
        }
        catch (Exception e)
        {
          completion.TrySetException(e);
        }
      });

      return completion;
    }

    /// <summary>
    /// Task delegate processor
    /// </summary>
    /// <param name="action"></param>
    public virtual TaskCompletionSource<T> Send<T>(Func<Task<T>> action)
    {
      var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

      Enqueue(() =>
      {
        try
        {
          completion.TrySetResult(action().GetAwaiter().GetResult());
        }
        catch (Exception e)
        {
          completion.TrySetException(e);
        }
      });

      return completion;
    }

    /// <summary>
    /// Task processor
    /// </summary>
    /// <param name="action"></param>
    public virtual TaskCompletionSource Send(Task action)
    {
      var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

      Enqueue(() =>
      {
        try
        {
          action.GetAwaiter().GetResult();
          completion.TrySetResult();
        }
        catch (Exception e)
        {
          completion.TrySetException(e);
        }
      });

      return completion;
    }

    /// <summary>
    /// Task processor
    /// </summary>
    /// <param name="action"></param>
    public virtual TaskCompletionSource<T> Send<T>(Task<T> action)
    {
      var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

      Enqueue(() =>
      {
        try
        {
          completion.TrySetResult(action.GetAwaiter().GetResult());
        }
        catch (Exception e)
        {
          completion.TrySetException(e);
        }
      });

      return completion;
    }

    /// <summary>
    /// Enqueue
    /// </summary>
    /// <param name="action"></param>
    protected virtual void Enqueue(Action action)
    {
      if (_queue.Count > _count)
      {
        _queue.TryTake(out _);
      }

      _queue.TryAdd(action);
    }
  }
}
