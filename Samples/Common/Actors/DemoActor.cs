using Distribution.AttributeSpace;
using System.Threading.Tasks;

namespace Common
{
  public class DemoActor
  {
    [Subscription]
    public virtual Task<DemoResponse> Create(CreateMessage message)
    {
      return Task.FromResult(new DemoResponse { Data = "Hello" });
    }

    [Subscription]
    public virtual Task<DemoResponse> Update(UpdateMessage message)
    {
      return Task.FromResult(new DemoResponse { Data = "World" });
    }

    [Subscription]
    public virtual Task Subscribe(dynamic message)
    {
      return Task.CompletedTask;
    }
  }
}
