using Common;
using Distribution.DomainSpace;
using System;

namespace Tests
{
  [TestClass]
  public class Concurrency
  {
    [TestMethod]
    public void RunStandardScheduler()
    {
      var scene = new Scene();
      var x1 = Thread.CurrentThread.ManagedThreadId;
      var x2 = scene.Send<DemoResponse>("A", new ProcessMessage()).Result.Id;
      var x3 = scene.Send<DemoResponse>("B", new ProcessMessage()).Result.Id;
      var x4 = Task.Run(() => scene.Send<DemoResponse>("C", new ProcessMessage()).Result.Id).Result;

      Assert.AreEqual(x1, x2);
      Assert.AreEqual(x2, x3);
      Assert.AreNotEqual(x3, x4);
    }

    [TestMethod]
    public void RunCustomScheduler()
    {
      var scene = new Scene();
      var scheduler = scene.Scheduler;

      Func<string, Task<DemoResponse>> syncActor = name => scene.Send<DemoResponse>(name, new ProcessMessage());

      var processId = Thread.CurrentThread.ManagedThreadId;
      var asyncProcessId = scheduler.Send(() => Thread.CurrentThread.ManagedThreadId).Task;
      var asyncActor = scheduler.Send(() => syncActor("B").Result.Id).Task;
      var asyncActorInsideTask = Task.Run(() => scheduler.Send(() => syncActor("C").Result.Id).Task);

      Assert.AreEqual(processId, syncActor("A").Result.Id);
      Assert.AreNotEqual(processId, asyncProcessId.Result);
      Assert.AreEqual(asyncProcessId.Result, asyncActor.Result);
      Assert.AreEqual(asyncProcessId.Result, asyncActorInsideTask.Result);
    }

    [TestMethod]
    public void RunStream()
    {
      var scene = new Scene();
      var message = new CountMessage { Id = 5 };

      scene.Subscribe<CountMessage>(o => Assert.AreEqual(message.Id, o.Id));
      scene.Subscribe<DemoResponse>(o => Assert.AreEqual(message.Id, o.Id));
      scene.Subscribe<Scene>(o => throw new Exception("Message does not exist"));
    }
  }
}
