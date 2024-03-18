using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Distribution.ServiceSpace
{
  public class ScheduleService : IDisposable
  {
    protected int _count;
    protected Channel<Action> _queue;
    protected CancellationTokenSource _cancellation;
    protected ManualResetEvent _semaphore = new ManualResetEvent(true);

    /// <summary>
    /// Constructor
    /// </summary>
    public ScheduleService() : this(1, TaskScheduler.Default, new CancellationTokenSource())
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
      _count = count;
      _cancellation = cancellation;
      _queue = Channel.CreateBounded<Action>(count);

      Task
        .Factory
        .StartNew(() =>
        {
          while (cancellation.IsCancellationRequested is false)
          {
            _semaphore.WaitOne();

            while (_queue.Reader.TryRead(out var action))
            {
              action();
            }

            _semaphore.Reset();
          }
        },
        cancellation.Token,
        TaskCreationOptions.LongRunning,
        scheduler);
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public virtual void Dispose()
    {
      _cancellation?.Cancel();
      _queue?.Writer?.TryComplete();
    }

    /// <summary>
    /// Action processor
    /// </summary>
    /// <param name="action"></param>
    public virtual TaskCompletionSource<bool> Send(Action action)
    {
      var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

      Enqueue(() =>
      {
        try
        {
          action();
          completion.TrySetResult(true);
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
    public virtual TaskCompletionSource<bool> Send(Func<Task> action)
    {
      var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

      Enqueue(() =>
      {
        try
        {
          action().GetAwaiter().GetResult();
          completion.TrySetResult(true);
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
    public virtual TaskCompletionSource<bool> Send(Task action)
    {
      var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

      Enqueue(() =>
      {
        try
        {
          action.GetAwaiter().GetResult();
          completion.TrySetResult(true);
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
      if (_queue.Reader.Count >= _count)
      {
        _queue.Reader.TryRead(out _);
      }

      _queue.Writer.WriteAsync(action);
      _semaphore.Set();
    }
  }
}
