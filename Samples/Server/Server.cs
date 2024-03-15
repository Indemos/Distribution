using Common;
using Distribution.Cluster.CommunicatorSpace;
using Distribution.Cluster.DomainSpace;
using Distribution.DomainSpace;
using Distribution.ServiceSpace;
using System;
using System.Timers;

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

      var scene = new ClusterScene();

      // Create basic HTTP JSON transport for peer-to-peer communication 

      var communicator = new RouteCommunicator
      {
        Scene = scene
      };

      // Create local server

      var service = new Distribution.Cluster.DomainSpace.Server
      {
        Port = port,
        Route = route,
        Communicator = communicator
      };

      service.Run();

      // Create beacon for service discovery, start UDP broadcasting of "Server" to notify other peers

      var beacon = new Beacon
      {
        Port = port
      };

      // beacon.Locate("Chain", port, TimeSpan.FromSeconds(1));

      var interval = new Timer(TimeSpan.FromSeconds(1));
      var scheduler = InstanceService<ScheduleService>.Instance;

      interval.Enabled = true;
      interval.Elapsed += (sender, e) => scheduler.Send(() => beacon.Send("Chain", port));

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
