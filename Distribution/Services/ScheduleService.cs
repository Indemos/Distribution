using Distribution.Models;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Distribution.Services
{
  public class ScheduleService : IDisposable
  {
    protected int _count;
    protected OptionModel _option;
    protected ManualResetEvent _semaphore;
    protected Channel<ActionModel> _queue;
    protected CancellationTokenSource _cancellation;

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
    /// <param name="cancellation"></param>
    public ScheduleService(int count, TaskScheduler scheduler, CancellationTokenSource cancellation)
    {
      _count = count;
      _cancellation = cancellation;
      _semaphore = new ManualResetEvent(true);
      _queue = Channel.CreateBounded<ActionModel>(Environment.ProcessorCount * 100);
      _option = new OptionModel { IsRemovable = true };

      Task.Factory.StartNew(() =>
      {
        while (cancellation?.IsCancellationRequested is false)
        {
          _semaphore?.WaitOne();

          while (_queue?.Reader?.TryRead(out var actionModel) ?? false)
          {
            actionModel.Action();
          }

          _semaphore?.Reset();
        }
      }, cancellation?.Token ?? CancellationToken.None, TaskCreationOptions.LongRunning, scheduler);
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public virtual void Dispose()
    {
      _cancellation?.Cancel();
      _semaphore?.Dispose();
      _cancellation?.Dispose();
      _queue?.Writer?.TryComplete();

      _queue = null;
      _semaphore = null;
      _cancellation = null;
    }

    /// <summary>
    /// Action processor
    /// </summary>
    /// <param name="action"></param>
    /// <param name="option"></param>
    public virtual TaskCompletionSource<bool> Send(Action action, OptionModel option = null)
    {
      var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      var actionModel = new ActionModel
      {
        Option = option,
        Action = () =>
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
        }
      };

      Enqueue(actionModel);

      return completion;
    }

    /// <summary>
    /// Delegate processor
    /// </summary>
    /// <param name="action"></param>
    /// <param name="option"></param>
    public virtual TaskCompletionSource<T> Send<T>(Func<T> action, OptionModel option = null)
    {
      var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
      var actionModel = new ActionModel
      {
        Option = option,
        Action = () =>
        {
          try
          {
            completion.TrySetResult(action());
          }
          catch (Exception e)
          {
            completion.TrySetException(e);
          }
        }
      };

      Enqueue(actionModel);

      return completion;
    }

    /// <summary>
    /// Delegate processor
    /// </summary>
    /// <param name="action"></param>
    /// <param name="option"></param>
    public virtual TaskCompletionSource<bool> Send(Func<Task> action, OptionModel option = null)
    {
      var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      var actionModel = new ActionModel
      {
        Option = option,
        Action = () =>
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
        }
      };

      Enqueue(actionModel);

      return completion;
    }

    /// <summary>
    /// Task delegate processor
    /// </summary>
    /// <param name="action"></param>
    /// <param name="option"></param>
    public virtual TaskCompletionSource<T> Send<T>(Func<Task<T>> action, OptionModel option = null)
    {
      var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
      var actionModel = new ActionModel
      {
        Option = option,
        Action = () =>
        {
          try
          {
            completion.TrySetResult(action().GetAwaiter().GetResult());
          }
          catch (Exception e)
          {
            completion.TrySetException(e);
          }
        }
      };

      Enqueue(actionModel);

      return completion;
    }

    /// <summary>
    /// Task processor
    /// </summary>
    /// <param name="action"></param>
    /// <param name="option"></param>
    public virtual TaskCompletionSource<bool> Send(Task action, OptionModel option = null)
    {
      var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      var actionModel = new ActionModel
      {
        Option = option,
        Action = () =>
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
        }
      };

      Enqueue(actionModel);

      return completion;
    }

    /// <summary>
    /// Task processor
    /// </summary>
    /// <param name="action"></param>
    /// <param name="option"></param>
    public virtual TaskCompletionSource<T> Send<T>(Task<T> action, OptionModel option = null)
    {
      var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
      var actionModel = new ActionModel
      {
        Option = option,
        Action = () =>
        {
          try
          {
            completion.TrySetResult(action.GetAwaiter().GetResult());
          }
          catch (Exception e)
          {
            completion.TrySetException(e);
          }
        }
      };

      Enqueue(actionModel);

      return completion;
    }

    /// <summary>
    /// Enqueue
    /// </summary>
    /// <param name="actionModel"></param>
    protected virtual void Enqueue(ActionModel actionModel)
    {
      try
      {
        if (_queue.Reader.TryPeek(out var previousAction))
        {
          if ((previousAction.Option ?? _option).IsRemovable && _queue.Reader.Count >= _count)
          {
            _queue.Reader.TryRead(out _);
          }
        }

        _queue.Writer.WriteAsync(actionModel);
        _semaphore.Set();
      }
      catch (ObjectDisposedException) {}
    }
  }
}
