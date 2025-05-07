using Distribution.Models;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Distribution.Services
{
  public class ScheduleService : IDisposable
  {
    protected int count;
    protected Thread process;
    protected Channel<ActionModel> queue;
    protected CancellationTokenSource cancellation;

    /// <summary>
    /// Constructor
    /// </summary>
    public ScheduleService() : this(1, new CancellationTokenSource())
    {
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="count"></param>
    /// <param name="cancellation"></param>
    public ScheduleService(int count, CancellationTokenSource cancellation)
    {
      this.count = count;
      this.cancellation = cancellation;
      this.queue = Channel.CreateBounded<ActionModel>(Environment.ProcessorCount * 100);

      process = new Thread(() =>
      {
        try
        {
          foreach (var actionModel in queue.Reader.ReadAllAsync(cancellation.Token).ToBlockingEnumerable())
          {
            actionModel.Run();
          }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
      })
      {
        IsBackground = true
      };

      process.Start();
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public virtual void Dispose()
    {
      try
      {
        cancellation?.Cancel();
        queue?.Writer?.TryComplete();
        cancellation?.Dispose();
        process?.Join();
      }
      catch { }
    }

    /// <summary>
    /// Action processor
    /// </summary>
    /// <param name="action"></param>
    /// <param name="removable"></param>
    public virtual TaskCompletionSource Send(Action action, bool removable = true)
    {
      var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      var actionModel = new ActionModel
      {
        Removable = removable,
        Dismiss = () => completion.TrySetResult(),
        Run = () =>
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
        }
      };

      Enqueue(actionModel);

      return completion;
    }

    /// <summary>
    /// Delegate processor
    /// </summary>
    /// <param name="action"></param>
    /// <param name="removable"></param>
    public virtual TaskCompletionSource<T> Send<T>(Func<T> action, bool removable = true)
    {
      var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
      var actionModel = new ActionModel
      {
        Removable = removable,
        Dismiss = () => completion.TrySetResult(default),
        Run = () =>
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
    /// <param name="removable"></param>
    public virtual TaskCompletionSource Send(Func<Task> action, bool removable = true)
    {
      var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      var actionModel = new ActionModel
      {
        Removable = removable,
        Dismiss = () => completion.TrySetResult(),
        Run = () =>
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
        }
      };

      Enqueue(actionModel);

      return completion;
    }

    /// <summary>
    /// Task delegate processor
    /// </summary>
    /// <param name="action"></param>
    /// <param name="removable"></param>
    public virtual TaskCompletionSource<T> Send<T>(Func<Task<T>> action, bool removable = true)
    {
      var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
      var actionModel = new ActionModel
      {
        Removable = removable,
        Dismiss = () => completion.TrySetResult(default),
        Run = () =>
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
    /// <param name="femovable"></param>
    public virtual TaskCompletionSource Send(Task action, bool femovable = true)
    {
      var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      var actionModel = new ActionModel
      {
        Removable = femovable,
        Dismiss = () => completion.TrySetResult(),
        Run = () =>
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
        }
      };

      Enqueue(actionModel);

      return completion;
    }

    /// <summary>
    /// Task processor
    /// </summary>
    /// <param name="action"></param>
    /// <param name="removable"></param>
    public virtual TaskCompletionSource<T> Send<T>(Task<T> action, bool removable = true)
    {
      var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
      var actionModel = new ActionModel
      {
        Removable = removable,
        Dismiss = () => completion.TrySetResult(default),
        Run = () =>
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
        if (queue.Reader.TryPeek(out var previousAction))
        {
          if (previousAction.Removable && queue.Reader.Count >= count && queue.Reader.TryRead(out var action))
          {
            action.Dismiss();
          }
        }

        queue.Writer.WriteAsync(actionModel);
      }
      catch (OperationCanceledException) { }
      catch (ObjectDisposedException) { }
    }
  }
}
