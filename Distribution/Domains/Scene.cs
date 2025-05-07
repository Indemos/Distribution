using Distribution.Attributes;
using Distribution.Models;
using Distribution.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Distribution.Domains
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
    protected IDictionary<string, Type> messages;

    /// <summary>
    /// Activations
    /// </summary>
    protected IDictionary<string, object> instances;

    /// <summary>
    /// Observers
    /// </summary>
    protected IDictionary<string, ActorModel> observers;

    /// <summary>
    /// Observers that can provide response
    /// </summary>
    protected IDictionary<string, ActorModel> processors;

    /// <summary>
    /// Message subscribers
    /// </summary>
    protected IDictionary<string, Action<object>> subscribers;

    /// <summary>
    /// Scheduler
    /// </summary>
    public virtual ScheduleService Scheduler { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public Scene()
    {
      messages = new ConcurrentDictionary<string, Type>();
      instances = new ConcurrentDictionary<string, object>();
      observers = new ConcurrentDictionary<string, ActorModel>();
      processors = new ConcurrentDictionary<string, ActorModel>();
      subscribers = new ConcurrentDictionary<string, Action<object>>();

      Scheduler = new ScheduleService();

      CreateProcessors();
      CreateObservers();
    }

    /// <summary>
    /// Get message descriptor
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public virtual Type GetMessage(string name) => messages[name];

    /// <summary>
    /// Subscribe to messages
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public virtual void Subscribe<T>(Action<T> action)
    {
      var message = typeof(T).Name;

      switch (subscribers.ContainsKey(message))
      {
        case true: subscribers[message] += o => action((T)o); break;
        case false: subscribers[message] = o => action((T)o); break;
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

      if (subscribers.TryGetValue(descriptor, out var subscriber))
      {
        subscriber(message);
      }

      var observers = this.observers.Select(observer => Scheduler.Send(() => 
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

      if (subscribers.TryGetValue(descriptor, out var subscriber))
      {
        subscriber(message);
      }

      if (processors.TryGetValue(descriptor, out var processor))
      {
        response = Scheduler.Send(() =>
        {
          var actor = processor
            .Descriptor
            .Invoke(GetInstance(name, processor.Descriptor), new[] { message }) as Task<T>;

          return actor.GetAwaiter().GetResult();

        }).Task;

        if (subscribers.TryGetValue(response.GetType().Name, out var responseSubscriber))
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
      messages?.Clear();
      instances?.Clear();
      observers?.Clear();
      processors?.Clear();
      subscribers?.Clear();

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
      if (instances.ContainsKey(name))
      {
        return instances[name];
      }

      return instances[name] = Activator.CreateInstance(processor.DeclaringType);
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
          descriptor.ReturnType.GetMethod(nameof(Task.GetAwaiter)) != null,
          message != null
        };

        if (conditions.All(o => o))
        {
          messages[message.ParameterType.FullName] = message.ParameterType;
          processors[message.ParameterType.Name] = new ActorModel
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
          descriptor.ReturnType.GetMethod(nameof(Task.GetAwaiter)) != null,
          message != null
        };

        if (conditions.All(o => o))
        {
          observers[descriptor.DeclaringType.FullName] = new ActorModel
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
