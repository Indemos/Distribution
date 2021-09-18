using Distribution.AttributeSpace;
using Distribution.DomainSpace;

namespace Server
{
  public interface IDemoActor : IActor
  {
  }

  public class DemoActor : Actor, IDemoActor
  {
    [Subscription(Message = typeof(CreateMessage))]
    public virtual Task<DemoResponse> Create(CreateMessage message)
    {
      return Task.FromResult(new DemoResponse());
    }

    [Subscription(Message = typeof(UpdateMessage))]
    public virtual Task<DemoResponse> Update(UpdateMessage message)
    {
      return Task.FromResult(new DemoResponse());
    }

    [Subscription]
    public virtual Task Subscribe(dynamic message)
    {
      return Task.CompletedTask;
    }
  }
}