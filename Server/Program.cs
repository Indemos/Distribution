using Distribution.CommunicatorSpace;
using Distribution.DomainSpace;

namespace Server
{
  public class Program
  {
    public static void Main(string[] args)
    {
      // Create client

      var clientSystem = new Scene();

      // Create client cluster

      var clientCommunicator = new RouteCommunicator();
      var clientService = new Service
      { 
        Scene = clientSystem,
        Communicator = clientCommunicator
      };

      clientService.Run();

      var clientCluster = new Cluster 
      {
        Port = 3000,
        Message = "ClientCluster"
      };

      clientCluster.CreateStream.Subscribe(o => Console.WriteLine(o.Name + " " + o.Address + " " + o.Port));
      clientCluster.DeleteStream.Subscribe(o => Console.WriteLine(o.Name + " " + o.Address + " " + o.Port));

      clientCluster.Subscribe("ClientCluster");
      clientCluster.SendInterval();

      // Create server 

      Console.ReadKey();

      clientCluster.Dispose();
    }
  }
}
