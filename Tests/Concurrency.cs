using Common;
using Distribution.DomainSpace;
using Distribution.ServiceSpace;

namespace UnitTests
{
  public class Concurrency
  {
    [Fact]
    public async Task RunStandardScheduler()
    {
      var scene = new Scene();
      var x1 = Environment.CurrentManagedThreadId;
      var x2 = (await scene.Send<DemoResponse>("A", new ProcessMessage())).Id;
      var x3 = (await scene.Send<DemoResponse>("B", new ProcessMessage())).Id;
      var x4 = (await Task.Run(() => scene.Send<DemoResponse>("C", new ProcessMessage()))).Id;

      Assert.Equal(x1, x2);
      Assert.Equal(x2, x3);
      Assert.NotEqual(x3, x4);
    }

    [Fact]
    public async Task RunCustomScheduler()
    {
      var scene = new Scene();
      var scheduler = InstanceService<ScheduleService>.Instance;

      Func<string, Task<DemoResponse>> syncActor = name => scene.Send<DemoResponse>(name, new ProcessMessage());

      var processId = Environment.CurrentManagedThreadId;
      var asyncProcessId = await scheduler.Send(() => Environment.CurrentManagedThreadId).Task;
      var asyncActor = (await scheduler.Send(() => syncActor("B")).Task).Id;
      var asyncActorPool = (await Task.Run(() => scheduler.Send(() => syncActor("C")).Task)).Id;

      //Assert.Equal(processId, syncActor("A"));
      //Assert.NotEqual(processId, asyncProcessId.Result);
      //Assert.Equal(asyncProcessId.Result, asyncActor.Result);
      //Assert.Equal(asyncProcessId.Result, asyncActorPool.Result);
    }

    [Fact]
    public void RunStream()
    {
      var scene = new Scene();
      var message = new CountMessage { Id = 5 };

      scene.Subscribe<CountMessage>(o => Assert.Equal(message.Id, o.Id));
      scene.Subscribe<DemoResponse>(o => Assert.Equal(message.Id, o.Id));
      scene.Subscribe<Scene>(o => throw new Exception("Message does not exist"));
    }
  }
}
