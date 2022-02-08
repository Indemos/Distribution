using Common;
using Distribution.CommunicatorSpace;
using Distribution.DomainSpace;
using System;
using System.Diagnostics;

namespace ClientSpace
{
  public class Client
  {
    public static void Main(string[] args)
    {
      SendMessageToLocalActor();
      SendMessageToVirtualClusterActor();

      Console.ReadKey();
    }

    /// <summary>
    /// Example of using actors within one app
    /// </summary>
    /// <returns></returns>
    static void SendMessageToLocalActor()
    {
      var scene = new Scene();
      var message = new CreateMessage { Name = "Local Message" };

      Stopwatch stopWatch = new Stopwatch();
      stopWatch.Start();

      DemoResponse response = null;

      for (var i = 0; i < 100000; i++)
      {
        response = scene.Send<DemoResponse>("Local Actor", message).Result;
      }

      stopWatch.Stop();
      // Get the elapsed time as a TimeSpan value.
      TimeSpan ts = stopWatch.Elapsed;

      // Format and display the TimeSpan value.
      string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
          ts.Hours, ts.Minutes, ts.Seconds,
          ts.Milliseconds / 10);
      Console.WriteLine("RunTime " + elapsedTime);
      Console.WriteLine("Local Response : " + response.Data);
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

      // Create beacon for service discovery, start UDP broadcasting of "Client" and subscribe to broadcasting from "Server" peers

      var beacon = new Beacon
      {
        Port = port,
        Message = "Client"
      };

      beacon.Subscribe("Server");

      // Make the current app a part of a cluster and provide preferred communicator

      var cluster = new Cluster
      {
        Route = route,
        Beacon = beacon,
        Communicator = communicator
      };

      // Start sending messages to random actors in a cluster

      var aTimer = new System.Timers.Timer(5000);

      aTimer.Enabled = true;
      aTimer.AutoReset = false;
      aTimer.Elapsed += (sender, e) =>
      {
        var response = cluster.Send<DemoResponse>("Virtual Cluster Actor", message).Result;

        Console.WriteLine("Cluster Response : " + response.Data);
      };
    }
  }
}
