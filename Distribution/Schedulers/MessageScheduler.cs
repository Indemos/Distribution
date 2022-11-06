using System;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;

namespace Distribution.SchedulerSpace
{
  public interface IMessageScheduler : IScheduler, IDisposable
  {
    /// <summary>
    /// Action processor
    /// </summary>
    /// <param name="action"></param>
    Task<T> Send<T>(Func<T> action);
  }

  public class MessageScheduler : IMessageScheduler
  {
    /// <summary>
    /// Scheduler date
    /// </summary>
    public virtual DateTimeOffset Now => DateTime.Now;

    /// <summary>
    /// Scheduler
    /// </summary>
    public virtual EventLoopScheduler Instance { get; protected set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public MessageScheduler()
    {
      Instance = new EventLoopScheduler(o => new Thread(o)
      {
        IsBackground = true,
        Priority = ThreadPriority.Highest,
        Name = nameof(MessageScheduler)
      });
    }

    /// <summary>
    /// Action processor
    /// </summary>
    /// <param name="action"></param>
    public virtual Task<T> Send<T>(Func<T> action)
    {
      var completion = new TaskCompletionSource<T>();

      Instance.Schedule(() => completion.SetResult(action.Invoke()));

      return completion.Task;
    }

    /// <summary>
    /// Schedule wrapper
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="state"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public virtual IDisposable Schedule<T>(T state, Func<IScheduler, T, IDisposable> action) => Instance.Schedule(state, action);

    /// <summary>
    /// Schedule wrapper
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <param name="state"></param>
    /// <param name="dueTime"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public virtual IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action) => Instance.Schedule(state, dueTime, action);

    /// <summary>
    /// Schedule wrapper
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <param name="state"></param>
    /// <param name="dueTime"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public virtual IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action) => Instance.Schedule(state, dueTime, action);

    /// <summary>
    /// Dispose
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public void Dispose()
    {
      Instance?.Dispose();
    }
  }
}
