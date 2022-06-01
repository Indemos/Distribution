using Common;
using Distribution.DomainSpace;

namespace Tests
{
  [TestClass]
  public class Concurrency
  {
    [TestMethod]
    public void Actions()
    {
      new Loader();

      var scene = new Scene();
      var id = Thread.CurrentThread.ManagedThreadId;
      var x1 = scene.Send<DemoResponse>("A", new ProcessMessage()).Result.Id;
      //var x2 = scene.SendAsync<DemoResponse>("B", new ProcessMessage()).Result.Id;
      //var x3 = Task.Run(() => scene.SendAsync<DemoResponse>("C", new ProcessMessage()).Result.Id).Result;

      Assert.AreEqual(id, x1);
      //Assert.AreEqual(x1, x2);
      //Assert.AreEqual(x2, x3);
    }
  }
}
