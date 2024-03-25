using Distribution.Domains;
using System.Threading.Tasks;

namespace Distribution.Cluster.Domains
{
  public class ClusterScene : Scene, IScene
  {
    public override Task<T> Send<T>(string name, object message)
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
  }
}
