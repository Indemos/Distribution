using Common;
using Distribution.DomainSpace;
using System.Reactive.Linq;

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
      var x1 = Thread.CurrentThread.ManagedThreadId;
      var x2 = scene.Send<DemoResponse>("A", new ProcessMessage(), scheduler).Result.Id;
      var x3 = Task.Run(() => scene.Send<DemoResponse>("B", new ProcessMessage(), scheduler).Result.Id).Result;
      var x4 = Task.Run(() => scene.Send<DemoResponse>("C", new ProcessMessage(), scheduler).Result.Id).Result;

      Assert.AreNotEqual(x1, x2);
      Assert.AreEqual(x2, x3);
      Assert.AreEqual(x3, x4);
    }

    [TestMethod]
    public void RunStream()
    {
      var id = -1;
      var scene = new Scene();
      var message = new CountMessage { Id = 5 };

      scene.Subscribe<CountMessage>(o => Assert.AreEqual(message.Id, o.Id));
      scene.Subscribe<DemoResponse>(o => Assert.AreEqual(message.Id, o.Id));
      scene.Subscribe<Scene>(o => throw new Exception("Message does not exist"));

      id = scene.Send<DemoResponse>("A", message).Result.Id;
    }
  }
}
