using Distribution.AttributeSpace;
using Distribution.ModelSpace;
using System;
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
    IDictionary<string, IRouteModel> Observers { get; }

    /// <summary>
    /// Observers that can provide response
    /// </summary>
    IDictionary<string, IRouteModel> Processors { get; }

    /// <summary>
    /// Get instance by composite index
    /// </summary>
    /// <param name="name"></param>
    /// <param name="descriptor"></param>
    /// <param name="processor"></param>
    /// <returns></returns>
    object GetInstance(string name, string descriptor, IRouteModel processor);

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
    public virtual IDictionary<string, IRouteModel> Observers { get; protected set; }

    /// <summary>
    /// Observers that can provide response
    /// </summary>
    public virtual IDictionary<string, IRouteModel> Processors { get; protected set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public Scene()
    {
      Messages = new Dictionary<string, Type>();
      Instances = new Dictionary<string, object>();
      Observers = new Dictionary<string, IRouteModel>();
      Processors = new Dictionary<string, IRouteModel>();

      CreateMaps();
    }

    /// <summary>
    /// Get instance by composite index
    /// </summary>
    /// <param name="name"></param>
    /// <param name="descriptor"></param>
    /// <param name="processor"></param>
    /// <returns></returns>
    public virtual object GetInstance(string name, string descriptor, IRouteModel processor)
    {
      var index = $"{ name }:{ descriptor }";

      if (Instances.TryGetValue(index, out object actor))
      {
        return actor;
      }

      return Instances[index] = processor.Creator.DynamicInvoke();
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

      if (Processors.TryGetValue(descriptor, out IRouteModel route))
      {
        //response = processor.Invoke(GetInstance(name, descriptor, processor), inputs);
        //response = route.Processor.DynamicInvoke(GetInstance(name, descriptor, route), message);
        response = route.Processor.DynamicInvoke(message);
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
      var messageName = message.GetType().Name;

      Parallel.ForEach(Observers.Values, async observer =>
      {
        var actor = GetInstance(name, messageName, observer);
        var processor = observer.Processor.DynamicInvoke(message) as Task;

        await processor;
      });
    }

    /// <summary>
    /// Create mapping between actors ans messages
    /// </summary>
    protected void CreateMaps()
    {
      var processors = AppDomain
        .CurrentDomain
        .GetAssemblies()
        .SelectMany(o => o.GetTypes())
        .SelectMany(o => o.GetMethods())
        .Where(descriptor =>
        {
          var message = descriptor
            .GetParameters()
            .ElementAtOrDefault(0);

          var conditions = new[]
          {
            descriptor.IsPublic,
            descriptor.GetCustomAttributes(typeof(Subscription), true).Any(),
            descriptor.ReturnType.GetMethod(nameof(Task.GetAwaiter)) is not null,
            message is not null
          };

          if (conditions.All(o => o))
          {
            var route = new RouteModel
            {
              Descriptor = descriptor,
              Processor = CreateProcessor(descriptor),
              Creator = CreateConstructor(descriptor)
            };

            Processors[message.ParameterType.Name] = route;
            Observers[descriptor.DeclaringType.FullName] = route;
            Messages[message.ParameterType.FullName] = message.ParameterType;

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
    protected Delegate CreateProcessor(MethodInfo descriptor)
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

    /// <summary>
    /// Compile processor into delegate
    /// </summary>
    /// <param name="descriptor"></param>
    /// <returns></returns>
    //protected Delegate CreateProcessor(MethodInfo descriptor)
    //{
    //  var instance = Expression.Parameter(descriptor.DeclaringType, descriptor.Name);

    //  var inputs = descriptor
    //    .GetParameters()
    //    .Select(o => Expression.Parameter(o.ParameterType, o.Name))
    //    .ToArray();

    //  var processor = Expression.Call(
    //    instance,
    //    descriptor,
    //    inputs
    //  );

    //  return Expression.Lambda(
    //    Expression.Convert(processor, descriptor.ReturnType),
    //    new[] { instance }.Concat(inputs)
    //  ).Compile();
    //}

    /// <summary>
    /// Create instance via reflection
    /// </summary>
    /// <param name="descriptor"></param>
    /// <returns></returns>
    protected Delegate CreateConstructor(MethodInfo descriptor)
    {
      var creator = descriptor.DeclaringType.GetConstructor(Array.Empty<Type>());
      var dataType = typeof(Func<>).MakeGenericType(descriptor.DeclaringType);

      return Expression.Lambda(dataType, Expression.New(creator)).Compile();
    }
  }
}
