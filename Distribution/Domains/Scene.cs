using Distribution.AttributeSpace;
using Distribution.SchedulerSpace;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading.Tasks;

namespace Distribution.DomainSpace
{
  /// <summary>
  /// Definition
  /// </summary>
  public interface IScene : IDisposable
  {
    /// <summary>
    /// Scheduler to execute tasks in a dedicated thread
    /// </summary>
    IMessageScheduler Scheduler { get; }

    /// <summary>
    /// Get message descriptor
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    Type GetMessage(string name);

    /// <summary>
    /// Subscribe to messages
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    void Subscribe<T>(Action<T> action);

    /// <summary>
    /// Distribute message among all actors
    /// </summary>
    /// <param name="message"></param>
    void Send(object message);

    /// <summary>
    /// Send message
    /// </summary>
    /// <param name="name"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    Task<T> Send<T>(string name, object message);

    /// <summary>
    /// Send message to separate process
    /// </summary>
    /// <param name="name"></param>
    /// <param name="message"></param>
    /// <param name="scheduler"></param>
    /// <returns></returns>
    Task<T> Send<T>(string name, object message, IMessageScheduler scheduler);
  }

  /// <summary>
  /// Implementation
  /// </summary>
  public class Scene : IScene
  {
    /// <summary>
    /// Messages
    /// </summary>
    protected IDictionary<string, Type> _messages = null;

    /// <summary>
    /// Activations
    /// </summary>
    protected IDictionary<string, object> _instances = null;

    /// <summary>
    /// Observers
    /// </summary>
    protected IDictionary<string, MethodInfo> _observers = null;

    /// <summary>
    /// Observers that can provide response
    /// </summary>
    protected IDictionary<string, MethodInfo> _processors = null;

    /// <summary>
    /// Message subscribers
    /// </summary>
    protected IDictionary<string, Action<object>> _subscribers = null;

    /// <summary>
    /// Scheduler to execute tasks in a dedicated thread
    /// </summary>
    public virtual IMessageScheduler Scheduler { get; protected set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public Scene()
    {
      Scheduler = new MessageScheduler();

      _messages = new ConcurrentDictionary<string, Type>();
      _instances = new ConcurrentDictionary<string, object>();
      _observers = new ConcurrentDictionary<string, MethodInfo>();
      _processors = new ConcurrentDictionary<string, MethodInfo>();
      _subscribers = new ConcurrentDictionary<string, Action<object>>();

      CreateProcessors();
      CreateObservers();
    }

    /// <summary>
    /// Get message descriptor
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public virtual Type GetMessage(string name)
    {
      return _messages[name];
    }

    /// <summary>
    /// Subscribe to messages
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public virtual void Subscribe<T>(Action<T> action)
    {
      _subscribers[typeof(T).Name] = o => action((T)o);
    }

    /// <summary>
    /// Distribute message among all actors
    /// </summary>
    /// <param name="message"></param>
    public virtual void Send(object message)
    {
      var inputs = new[] { message };
      var descriptor = message.GetType().Name;

      if (_subscribers.TryGetValue(descriptor, out Action<object> messageSubscriber))
      {
        messageSubscriber(message);
      }

      Parallel.ForEach(_observers, async observer =>
      {
        var actor = GetInstance(observer.Key, observer.Value);
        var processor = observer.Value.Invoke(actor, inputs) as Task;

        await processor;
      });
    }

    /// <summary>
    /// Send message
    /// </summary>
    /// <param name="name"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public virtual async Task<T> Send<T>(string name, object message)
    {
      T response = default;

      if (message is null)
      {
        return response;
      }

      var descriptor = message.GetType().Name;

      if (_subscribers.TryGetValue(descriptor, out Action<object> messageSubscriber))
      {
        messageSubscriber(message);
      }

      if (_processors.TryGetValue(descriptor, out MethodInfo processor))
      {
        dynamic actor = processor.Invoke(GetInstance(name, processor), new[] { message });

        response = (T)(await actor);

        if (_subscribers.TryGetValue(response.GetType().Name, out Action<object> responseSubscriber))
        {
          responseSubscriber(response);
        }
      }

      return response;
    }

    /// <summary>
    /// Send message to separate process
    /// </summary>
    /// <param name="name"></param>
    /// <param name="message"></param>
    /// <param name="scheduler"></param>
    /// <returns></returns>
    public virtual Task<T> Send<T>(string name, object message, IMessageScheduler scheduler)
    {
      return scheduler.Send(() => Send<T>(name, message).GetAwaiter().GetResult());
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public virtual void Dispose()
    {
      _messages?.Clear();
      _instances?.Clear();
      _observers?.Clear();
      _processors?.Clear();
      _subscribers?.Clear();

      Scheduler?.Dispose();
    }

    /// <summary>
    /// Get instance by composite index
    /// </summary>
    /// <param name="name"></param>
    /// <param name="processor"></param>
    /// <returns></returns>
    protected virtual object GetInstance(string name, MethodInfo processor)
    {
      if (_instances.ContainsKey(name))
      {
        return _instances[name];
      }

      return _instances[name] = Activator.CreateInstance(processor.DeclaringType);
    }

    /// <summary>
    /// Get actors
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<MethodInfo> GetActors()
    {
      return AppDomain
        .CurrentDomain
        .GetAssemblies()
        .SelectMany(o => o.GetTypes())
        .SelectMany(o => o.GetMethods());
    }

    /// <summary>
    /// Create processors
    /// </summary>
    protected virtual IEnumerable<MethodInfo> CreateProcessors()
    {
      return GetActors().Where(descriptor =>
      {
        var message = descriptor
          .GetParameters()
          .ElementAtOrDefault(0);

        var conditions = new[]
        {
          descriptor.IsPublic,
          descriptor.GetCustomAttributes(typeof(Processor), true).Any(),
          descriptor.ReturnType.GetMethod(nameof(Task.GetAwaiter)) is not null,
          message is not null
        };

        if (conditions.All(o => o))
        {
          _processors[message.ParameterType.Name] = descriptor;
          _messages[message.ParameterType.FullName] = message.ParameterType;

          return true;
        }

        return false;

      }).ToList();
    }

    /// <summary>
    /// Create observers
    /// </summary>
    protected virtual IEnumerable<MethodInfo> CreateObservers()
    {
      return GetActors().Where(descriptor =>
      {
        var message = descriptor
          .GetParameters()
          .ElementAtOrDefault(0);

        var conditions = new[]
        {
          descriptor.IsPublic,
          descriptor.GetCustomAttributes(typeof(Observer), true).Any(),
          descriptor.ReturnType.GetMethod(nameof(Task.GetAwaiter)) is not null,
          message is not null
        };

        if (conditions.All(o => o))
        {
          _observers[descriptor.DeclaringType.FullName] = descriptor;

          return true;
        }

        return false;

      }).ToList();
    }
  }
}
