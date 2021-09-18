using System.Reflection;
using Distribution.AttributeSpace;

namespace Distribution.DomainSpace
{
  /// <summary>
  /// Definition
  /// </summary>
  public interface IScene : IDisposable
  {
    /// <summary>
    /// Activations
    /// </summary>
    IDictionary<string, IActor> Actors { get; }

    /// <summary>
    /// Actors listening to broadcasted events
    /// </summary>
    IDictionary<string, MethodInfo> Observers { get; }

    /// <summary>
    /// Actors that can provide response
    /// </summary>
    IDictionary<string, IList<MethodInfo>> Responders { get; }

    /// <summary>
    /// Get instance by composite index
    /// </summary>
    /// <param name="session"></param>
    /// <param name="name"></param>
    /// <param name="processor"></param>
    /// <returns></returns>
    IActor GetInstance(string session, string name, MethodInfo processor);

    /// <summary>
    /// Send
    /// </summary>
    /// <param name="session"></param>
    /// <param name="name"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    IEnumerable<Task> Send(string session, string name, object message);
  }

  /// <summary>
  /// Implementation
  /// </summary>
  public class Scene : IScene
  {
    /// <summary>
    /// Activations
    /// </summary>
    public IDictionary<string, IActor> Actors { get; protected set; }

    /// <summary>
    /// Actors listening to broadcasted events
    /// </summary>
    public IDictionary<string, MethodInfo> Observers { get; protected set; }

    /// <summary>
    /// Actors that can provide response
    /// </summary>
    public IDictionary<string, IList<MethodInfo>> Responders { get; protected set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public Scene()
    {
      Actors = new Dictionary<string, IActor>();
      Observers = new Dictionary<string, MethodInfo>();
      Responders = new Dictionary<string, IList<MethodInfo>>();

      var processors = AppDomain
        .CurrentDomain
        .GetAssemblies()
        .SelectMany(o => o.GetTypes())
        .Where(o => o.IsInterface == false && o.IsAbstract == false && typeof(IActor).IsAssignableFrom(o))
        .SelectMany(o => o.GetMethods())
        .Where(o => o.ReturnType.GetMethod(nameof(Task.GetAwaiter)) is not null);

      foreach (var processor in processors)
      {
        var attributes = processor.GetCustomAttributes(typeof(Subscription), true);

        foreach (Subscription attribute in attributes)
        {
          var message = attribute.Message;
          var inputs = processor.GetParameters();

          if (inputs.Count() == 1)
          {
            var input = inputs.ElementAtOrDefault(0);

            if (Equals(message, input.ParameterType))
            {
              Responders.TryGetValue(message.Name, out IList<MethodInfo> actors);
              Responders[message.Name] = actors ?? new List<MethodInfo>();
              Responders[message.Name].Add(processor);
            }

            Observers[processor.DeclaringType.FullName] = processor;
          }
        }
      }
    }

    /// <summary>
    /// Get instance by composite index
    /// </summary>
    /// <param name="session"></param>
    /// <param name="name"></param>
    /// <param name="processor"></param>
    /// <returns></returns>
    public virtual IActor GetInstance(string session, string name, MethodInfo processor)
    {
      var index = $"{ session }:{ name }:{ processor.DeclaringType.FullName }";

      if (Actors.TryGetValue(index, out IActor actor))
      {
        return actor;
      }

      return Actors[index] = Activator.CreateInstance(processor.DeclaringType) as IActor;
    }

    /// <summary>
    /// Send
    /// </summary>
    /// <param name="session"></param>
    /// <param name="name"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public virtual IEnumerable<Task> Send(string session, string name, object message)
    {
      if (message == null)
      {
        return new List<Task>();
      }

      var processors = new List<Task>();
      var messageType = message.GetType();

      Responders.ForEach(index =>
      {
        if (Equals(messageType.Name, index.Key))
        {
          index.Value.ForEach(o =>
          {
            var actor = GetInstance(session, name, o);
            var processor = o.Invoke(actor, new[] { message }) as Task;

            processors.Add(processor);
          });
        }
      });

      Observers.ForEach(o =>
      {
        var actor = GetInstance(session, name, o.Value);
        var processor = o.Value.Invoke(actor, new[] { message }) as Task;

        processors.Add(processor);
      });

      return processors;
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public virtual void Dispose()
    {
      Actors.ForEach(o => o.Value.Dispose());
    }
  }
}