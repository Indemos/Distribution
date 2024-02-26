using Common;
using Distribution.DomainSpace;

namespace UnitTests
{
  public class Concurrency
  {
    [Fact]
    public void RunStandardScheduler()
    {
      var scene = new Scene();
      var x1 = Environment.CurrentManagedThreadId;
      var x2 = scene.Send<DemoResponse>("A", new ProcessMessage()).Result.Id;
      var x3 = scene.Send<DemoResponse>("B", new ProcessMessage()).Result.Id;
      var x4 = Task.Run(() => scene.Send<DemoResponse>("C", new ProcessMessage()).Result.Id).Result;

      Assert.Equal(x1, x2);
      Assert.Equal(x2, x3);
      Assert.NotEqual(x3, x4);
    }

    [Fact]
    public void RunCustomScheduler()
    {
      var scene = new Scene();
      var scheduler = scene.Scheduler;

      Func<string, Task<DemoResponse>> syncActor = name => scene.Send<DemoResponse>(name, new ProcessMessage());

      var processId = Environment.CurrentManagedThreadId;
      var asyncProcessId = scheduler.Send(() => Thread.CurrentThread.ManagedThreadId).Task;
      var asyncActor = scheduler.Send(() => syncActor("B").Result.Id).Task;
      var asyncActorInsideTask = Task.Run(() => scheduler.Send(() => syncActor("C").Result.Id).Task);

      Assert.Equal(processId, syncActor("A").Result.Id);
      Assert.NotEqual(processId, asyncProcessId.Result);
      Assert.Equal(asyncProcessId.Result, asyncActor.Result);
      Assert.Equal(asyncProcessId.Result, asyncActorInsideTask.Result);
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
