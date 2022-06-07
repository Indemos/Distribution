using Common;
using Distribution.CommunicatorSpace;
using Distribution.DomainSpace;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ClientSpace
{
  public class Client
  {
    public static async Task Main()
    {
      await SendMessageToLocalActor();
      SendMessageToVirtualClusterActor();

      Console.ReadKey();
    }

    /// <summary>
    /// Example of using actors within one app
    /// </summary>
    /// <returns></returns>
    static async Task SendMessageToLocalActor()
    {
      // Demo actor

      var scene = new Scene();
      var message = new CreateMessage { Name = "Local Message" };
      var response = await scene.Send<DemoResponse>("Local Actor", message);

      Console.WriteLine("Local Response : " + response.Data + Environment.NewLine);

      // Calculator actor

      Console.WriteLine("Calculator" + Environment.NewLine);

      var count = 5;

      Enumerable.Range(0, count).ForEach(async o =>
      {
        switch (true)
        {
          case true when o > count / 2:

            var inc = await scene.Send<OperationResponse>(nameof(CalculatorActor), new IncrementMessage { Input = o });
            Console.WriteLine("Op : " + inc.Operation + " " + inc.Value);
            break;

          case true when o < count / 2:

            var dec = await scene.Send<OperationResponse>(nameof(CalculatorActor), new DecrementMessage { Input = o });
            Console.WriteLine("Op : " + dec.Operation + " " + dec.Value);
            break;
        }
      });

      var calculation = await scene.Send<OperationResponse>(nameof(CalculatorActor), new SummaryMessage());

      Console.WriteLine(Environment.NewLine + "Amount : " + calculation.Value + Environment.NewLine);
    }

    /// <summary>
    /// Example of using distributed network of virtual actors within a cluster
    /// </summary>
    /// <returns></returns>
    static void SendMessageToVirtualClusterActor()
    {
      var scene = new Scene();
      var message = new UpdateMessage { Name = "Cluster Message" };

      // Create client cluster

      var port = 3000;
      var route = "/messages";

      // Create basic HTTP JSON transport for peer-to-peer communication 

      var communicator = new RouteCommunicator
      {
        Scene = scene
      };

      // Create beacon for service discovery, start UDP broadcasting of "Client" and subscribe to broadcasting from "Chain" network

      var beacon = new Beacon
      {
        Port = port
      };

      beacon.Locate("Chain", port, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));

      // Make the current app a part of a cluster and provide preferred communicator

      var cluster = new Cluster
      {
        Route = route,
        Beacon = beacon,
        Communicator = communicator
      };

      beacon.DropStream.Subscribe(o =>
      {
        Console.WriteLine("Drop : " + o.Address);
      });

      beacon.CreateStream.Subscribe(async o =>
      {
        var response = await cluster.Send<DemoResponse>("Virtual Cluster Actor", message);

        Console.WriteLine("Cluster Response : " + response.Data);
      });
    }
  }
}
