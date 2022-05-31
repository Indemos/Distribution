using Distribution.AttributeSpace;
using System;
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
    Task<TResponse> Send<TResponse>(string name, object message);
  }

  /// <summary>
  /// Implementation
  /// </summary>
  public class Scene : IScene
  {
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
      Messages = new Dictionary<string, Type>();
      Instances = new Dictionary<string, object>();
      Observers = new Dictionary<string, MethodInfo>();
      Processors = new Dictionary<string, MethodInfo>();

      CreateMaps();
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
    public virtual Task<TResponse> Send<TResponse>(string name, object message)
    {
      return Send(name, message) as Task<TResponse>;
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
    /// Create mapping between actors ans messages
    /// </summary>
    protected void CreateMaps()
    {
      var actors = AppDomain
        .CurrentDomain
        .GetAssemblies()
        .SelectMany(o => o.GetTypes())
        .SelectMany(o => o.GetMethods());

      var processors = actors.Where(processor =>
      {
        var message = processor
          .GetParameters()
          .ElementAtOrDefault(0);

        var conditions = new[]
        {
          processor.IsPublic,
          processor.GetCustomAttributes(typeof(Processor), true).Any(),
          processor.ReturnType.GetMethod(nameof(Task.GetAwaiter)) is not null,
          message is not null
        };

        if (conditions.All(o => o))
        {
          Processors[message.ParameterType.Name] = processor;
          Messages[message.ParameterType.FullName] = message.ParameterType;

          return true;
        }

        return false;

      }).ToList();

      var observers = actors.Where(processor =>
      {
        var message = processor
          .GetParameters()
          .ElementAtOrDefault(0);

        var conditions = new[]
        {
          processor.IsPublic,
          processor.GetCustomAttributes(typeof(Observer), true).Any(),
          processor.ReturnType.GetMethod(nameof(Task.GetAwaiter)) is not null,
          message is not null
        };

        if (conditions.All(o => o))
        {
          Observers[processor.DeclaringType.FullName] = processor;

          return true;
        }

        return false;

      }).ToList();
    }
  }
}
