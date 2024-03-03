using Distribution.AttributeSpace;
using Distribution.ModelSpace;
using Distribution.ServiceSpace;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
    /// Scheduler
    /// </summary>
    ScheduleService Scheduler { get; set; }

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
    Task Send(object message);

    /// <summary>
    /// Send message
    /// </summary>
    /// <param name="name"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    Task<T> Send<T>(string name, object message);
  }

  /// <summary>
  /// Implementation
  /// </summary>
  public class Scene : IScene
  {
    /// <summary>
    /// Messages
    /// </summary>
    protected IDictionary<string, Type> _messages;

    /// <summary>
    /// Activations
    /// </summary>
    protected IDictionary<string, object> _instances;

    /// <summary>
    /// Observers
    /// </summary>
    protected IDictionary<string, ActorModel> _observers;

    /// <summary>
    /// Observers that can provide response
    /// </summary>
    protected IDictionary<string, ActorModel> _processors;

    /// <summary>
    /// Message subscribers
    /// </summary>
    protected IDictionary<string, Action<object>> _subscribers;

    /// <summary>
    /// Scheduler
    /// </summary>
    public virtual ScheduleService Scheduler { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public Scene()
    {
      _messages = new ConcurrentDictionary<string, Type>();
      _instances = new ConcurrentDictionary<string, object>();
      _observers = new ConcurrentDictionary<string, ActorModel>();
      _processors = new ConcurrentDictionary<string, ActorModel>();
      _subscribers = new ConcurrentDictionary<string, Action<object>>();

      Scheduler = new ScheduleService();

      CreateProcessors();
      CreateObservers();
    }

    /// <summary>
    /// Get message descriptor
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public virtual Type GetMessage(string name) => _messages[name];

    /// <summary>
    /// Subscribe to messages
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public virtual void Subscribe<T>(Action<T> action)
    {
      var message = typeof(T).Name;

      switch (_subscribers.ContainsKey(message))
      {
        case true: _subscribers[message] += o => action((T)o); break;
        case false: _subscribers[message] = o => action((T)o); break;
      }
    }

    /// <summary>
    /// Distribute message among all actors
    /// </summary>
    /// <param name="message"></param>
    public virtual Task Send(object message)
    {
      var inputs = new[] { message };
      var descriptor = message.GetType().Name;

      if (_subscribers.TryGetValue(descriptor, out var subscriber))
      {
        subscriber(message);
      }

      var observers = _observers.Select(observer => Scheduler.Send(() => 
      {
        var actor = GetInstance(observer.Key, observer.Value.Descriptor);
        var processor = observer.Value.Descriptor.Invoke(actor, inputs) as Task;

        return processor;

      }).Task);

      return Task.WhenAll(observers);
    }

    /// <summary>
    /// Send message
    /// </summary>
    /// <param name="name"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public virtual Task<T> Send<T>(string name, object message)
    {
      Task<T> response = Task.FromResult<T>(default);

      if (message is null)
      {
        return response;
      }

      var descriptor = message.GetType().Name;

      if (_subscribers.TryGetValue(descriptor, out var subscriber))
      {
        subscriber(message);
      }

      if (_processors.TryGetValue(descriptor, out var processor))
      {
        response = Scheduler.Send(() =>
        {
          dynamic actor = processor
            .Descriptor
            .Invoke(GetInstance(name, processor.Descriptor), new[] { message });

          return (T)actor.GetAwaiter().GetResult();

        }).Task;

        if (_subscribers.TryGetValue(response.GetType().Name, out var responseSubscriber))
        {
          responseSubscriber(response);
        }
      }

      return response;
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
          _messages[message.ParameterType.FullName] = message.ParameterType;
          _processors[message.ParameterType.Name] = new ActorModel
          {
            Descriptor = descriptor
          };

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
          _observers[descriptor.DeclaringType.FullName] = new ActorModel
          {
            Descriptor = descriptor
          };

          return true;
        }

        return false;

      }).ToList();
    }

    /// <summary>
    /// Compile processor into delegate
    /// </summary>
    /// <param name="descriptor"></param>
    /// <returns></returns>
    protected virtual Delegate CreateAction(MethodInfo descriptor)
    {
      var arguments = descriptor
        .GetParameters()
        .Select(o => o.ParameterType)
        .Concat(new[] { descriptor.ReturnType })
        .ToArray();

      return descriptor.CreateDelegate(
        Expression.GetDelegateType(arguments),
        Activator.CreateInstance(descriptor.DeclaringType));
    }
  }
}
