using Distribution.AttributeSpace;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
  public class DemoActor
  {
    [Processor]
    public virtual Task<DemoResponse> Create(CreateMessage message)
    {
      return Task.FromResult(new DemoResponse { Data = "Local response" });
    }

    [Processor]
    public virtual Task<DemoResponse> Update(UpdateMessage message)
    {
      return Task.FromResult(new DemoResponse { Data = "Cluster response" });
    }

    [Processor]
    public virtual Task<DemoResponse> GetProcess(ProcessMessage message)
    {
      return Task.FromResult(new DemoResponse { Id = Thread.CurrentThread.ManagedThreadId });
    }

    [Observer]
    public virtual Task Subscribe(dynamic message)
    {
      return Task.CompletedTask;
    }
  }
}
