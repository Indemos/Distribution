using Common;
using Distribution.DomainSpace;

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
      var x1 = Thread.CurrentThread.ManagedThreadId;
      var x2 = scene.Schedule<DemoResponse>("A", new ProcessMessage()).Result.Id;
      var x3 = Task.Run(() => scene.Schedule<DemoResponse>("B", new ProcessMessage()).Result.Id).Result;
      var x4 = Task.Run(() => scene.Schedule<DemoResponse>("C", new ProcessMessage()).Result.Id).Result;

      Assert.AreNotEqual(x1, x2);
      Assert.AreEqual(x2, x3);
      Assert.AreEqual(x3, x4);
    }
  }
}
