using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Distribution.ServiceSpace
{
  public class ScheduleService : IDisposable
  {
    public virtual CancellationTokenSource Cancellation { get; set; }
    protected virtual BlockingCollection<Action> Queue { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="scheduler"></param>
    public ScheduleService() : this(TaskScheduler.Default)
    {
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="scheduler"></param>
    public ScheduleService(TaskScheduler scheduler)
    {
      Queue = new();
      Cancellation = new CancellationTokenSource();

      Task.Factory.StartNew(() =>
      {
        foreach (var action in Queue.GetConsumingEnumerable())
        {
          action();
        }
      },
      Cancellation.Token,
      TaskCreationOptions.LongRunning,
      scheduler ?? TaskScheduler.Current).ContinueWith(o => Queue.Dispose());
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public virtual void Dispose() => Cancellation?.Cancel();

    /// <summary>
    /// Action processor
    /// </summary>
    /// <param name="action"></param>
    public virtual TaskCompletionSource Send(Action action)
    {
      var completion = new TaskCompletionSource();

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
      var completion = new TaskCompletionSource<T>();

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
      var completion = new TaskCompletionSource();

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
      var completion = new TaskCompletionSource<T>();

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
      var completion = new TaskCompletionSource();

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
      var completion = new TaskCompletionSource<T>();

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
      if (Queue.TryTake(out var o))
      {
      }

      Queue.TryAdd(action);
    }
  }
}
