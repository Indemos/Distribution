using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Distribution.ServiceSpace
{
  public class ScheduleService : IDisposable
  {
    protected virtual CancellationTokenSource _cancellation { get; set; }

    protected virtual Channel<Action> _queue { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public ScheduleService() : this(1, TaskScheduler.Current, new CancellationTokenSource())
    {
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="count"></param>
    /// <param name="scheduler"></param>
    /// <param name="cancellation"></param>
    public ScheduleService(int count, TaskScheduler scheduler, CancellationTokenSource cancellation)
    {
      _cancellation = cancellation;
      _queue = Channel.CreateBounded<Action>(count);

      Task.Factory.StartNew(() =>
      {
        while (true)
        {
          if (_queue.Reader.TryRead(out var action))
          {
            action();
          }
        }
      },
      _cancellation.Token, TaskCreationOptions.LongRunning, scheduler).ContinueWith(o =>
      {
        _queue.Writer.TryComplete();
      });
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public virtual void Dispose() => _cancellation?.Cancel();

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
    protected virtual void Enqueue(Action action) => _queue.Writer.WriteAsync(action);
  }
}
