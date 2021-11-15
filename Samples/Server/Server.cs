using Common;
using Distribution.CommunicatorSpace;
using Distribution.DomainSpace;
using System;

namespace ServerSpace
{
  public class Server
  {
    public static void Main(string[] args)
    {
      // Create empty loader class to make sure "Common" assembly is loaded

      new Loader();

      var port = 3000;
      var route = "/messages";

      // Create actor system

      var scene = new Scene();

      // Create basic HTTP JSON transport for peer-to-peer communication 

      var communicator = new RouteCommunicator
      {
        Scene = scene
      };

      // Create local server

      var service = new Service
      {
        Port = port,
        Route = route,
        Communicator = communicator
      };

      service.Run();

      // Create beacon for service discovery, start UDP broadcasting of "Server" to notify other peers

      var beacon = new Beacon
      {
        Port = port,
        Message = "Server"
      };

      beacon.SendInterval();

      // Make the current app a part of a cluster and provide preferred communicator

      var cluster = new Cluster
      {
        Route = route,
        Beacon = beacon,
        Communicator = communicator
      };

      Console.ReadKey();

      // Dispose connections and other resources

      beacon?.Dispose();
      service?.Dispose();
      cluster?.Dispose();
    }
  }
}
