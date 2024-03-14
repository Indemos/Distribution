using Common;
using Distribution.DomainSpace;
using System;
using System.Threading.Tasks;

namespace Tests
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

      //Assert.NotEqual(x1, x2);
      //Assert.Equal(x2, x3);
      //Assert.Equal(x3, x4);
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
