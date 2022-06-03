using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Distribution.SchedulerSpace
{
  public class MessageScheduler
  {
    /// <summary>
    /// Awaitable action wrapper
    /// </summary>
    public class Item
    {
      public Delegate Action { get; set; }
      public CancellationTokenSource Cancellation { get; set; }
      public TaskCompletionSource<dynamic> Completion { get; set; }
    }

    /// <summary>
    /// Queue
    /// </summary>
    protected BlockingCollection<Item> _queue = new(new ConcurrentQueue<Item>());

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="scheduler"></param>
    /// <param name="source"></param>
    public MessageScheduler(TaskScheduler scheduler = null, CancellationTokenSource source = null)
    {
      var sc = scheduler ?? TaskScheduler.Default;
      var cancellation = source?.Token ?? CancellationToken.None;

      Task.Factory.StartNew(Consume, cancellation, TaskCreationOptions.LongRunning, sc);
    }

    /// <summary>
    /// Action processor
    /// </summary>
    /// <param name="action"></param>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    public virtual Task<dynamic> Send(Delegate action, CancellationTokenSource cancellation = null)
    {
      var item = new Item
      {
        Action = action,
        Cancellation = cancellation,
        Completion = new TaskCompletionSource<dynamic>()
      };

      _queue.Add(item);

      return item.Completion.Task;
    }

    /// <summary>
    /// Background process
    /// </summary>
    protected virtual void Consume()
    {
      foreach (var item in _queue.GetConsumingEnumerable())
      {
        var completion = item?.Completion;
        var cancellation = item?.Cancellation?.Token ?? CancellationToken.None;

        try
        {
          if (cancellation.IsCancellationRequested)
          {
            completion.SetCanceled();
            continue;
          }

          completion.SetResult(item.Action.DynamicInvoke());
        }
        catch (OperationCanceledException)
        {
          completion.SetCanceled();
        }
        catch (Exception e)
        {
          completion.SetException(e);
        }
      }
    }
  }
}
