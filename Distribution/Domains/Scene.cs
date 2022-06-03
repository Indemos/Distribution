using Distribution.AttributeSpace;
using Distribution.SchedulerSpace;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
    MessageScheduler Scheduler { get; }

    /// <summary>
    /// Messages
    /// </summary>
    IDictionary<string, Type> Messages { get; }

    /// <summary>
    /// Activations
    /// </summary>
    IDictionary<string, object> Instances { get; }

    /// <summary>
    /// Observers
    /// </summary>
    IDictionary<string, MethodInfo> Observers { get; }

    /// <summary>
    /// Observers that can provide response
    /// </summary>
    IDictionary<string, MethodInfo> Processors { get; }

    /// <summary>
    /// Get instance by composite index
    /// </summary>
    /// <param name="name"></param>
    /// <param name="processor"></param>
    /// <returns></returns>
    object GetInstance(string name, MethodInfo processor);

    /// <summary>
    /// Send message
    /// </summary>
    /// <param name="name"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    dynamic Send(string name, object message);

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
    /// Scheduler to execute tasks in a dedicated thread
    /// </summary>
    public virtual MessageScheduler Scheduler { get; protected set; }

    /// <summary>
    /// Messages
    /// </summary>
    public virtual IDictionary<string, Type> Messages { get; protected set; }

    /// <summary>
    /// Activations
    /// </summary>
    public virtual IDictionary<string, object> Instances { get; protected set; }

    /// <summary>
    /// Observers
    /// </summary>
    public virtual IDictionary<string, MethodInfo> Observers { get; protected set; }

    /// <summary>
    /// Observers that can provide response
    /// </summary>
    public virtual IDictionary<string, MethodInfo> Processors { get; protected set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public Scene()
    {
      Scheduler = new();
      Messages = new ConcurrentDictionary<string, Type>();
      Instances = new ConcurrentDictionary<string, object>();
      Observers = new ConcurrentDictionary<string, MethodInfo>();
      Processors = new ConcurrentDictionary<string, MethodInfo>();

      CreateProcessors();
      CreateObservers();
    }

    /// <summary>
    /// Get instance by composite index
    /// </summary>
    /// <param name="name"></param>
    /// <param name="processor"></param>
    /// <returns></returns>
    public virtual object GetInstance(string name, MethodInfo processor)
    {
      if (Instances.TryGetValue(name, out object actor))
      {
        return actor;
      }

      return Instances[name] = Activator.CreateInstance(processor.DeclaringType);
    }

    /// <summary>
    /// Send message
    /// </summary>
    /// <param name="name"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public virtual dynamic Send(string name, object message)
    {
      dynamic response = null;

      if (message is null)
      {
        return response;
      }

      var inputs = new[] { message };
      var descriptor = message.GetType().Name;

      if (Processors.TryGetValue(descriptor, out MethodInfo processor))
      {
        response = processor.Invoke(GetInstance(name, processor), inputs);
      }

      Distribute(name, message);

      return response;
    }

    /// <summary>
    /// Send message
    /// </summary>
    /// <param name="name"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public virtual Task<T> Send<T>(string name, object message)
    {
      return Send(name, message) as Task<T>;
    }

    /// <summary>
    /// Send message to separate process
    /// </summary>
    /// <param name="name"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public virtual Task<T> Schedule<T>(string name, object message)
    {
      var source = new TaskCompletionSource<T>();

      Scheduler.Send(() =>
      {
        source.SetResult(Send(name, message).GetAwaiter().GetResult());
      });

      return source.Task;
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public virtual void Dispose()
    {
    }

    /// <summary>
    /// Distribute message among all actors
    /// </summary>
    /// <param name="name"></param>
    /// <param name="message"></param>
    protected void Distribute(string name, object message)
    {
      var inputs = new[] { message };

      Observers.ForEach(async observer =>
      {
        var actor = GetInstance(observer.Key, observer.Value);
        var processor = observer.Value.Invoke(actor, inputs) as Task;

        await processor;
      });
    }

    /// <summary>
    /// Get actors
    /// </summary>
    /// <returns></returns>
    protected IEnumerable<MethodInfo> GetActors()
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
    protected IEnumerable<MethodInfo> CreateProcessors()
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
          Processors[message.ParameterType.Name] = descriptor;
          Messages[message.ParameterType.FullName] = message.ParameterType;

          return true;
        }

        return false;

      }).ToList();
    }

    /// <summary>
    /// Create observers
    /// </summary>
    protected IEnumerable<MethodInfo> CreateObservers()
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
          Observers[descriptor.DeclaringType.FullName] = descriptor;

          return true;
        }

        return false;

      }).ToList();
    }
  }
}
