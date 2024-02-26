using Distribution.CommunicatorSpace;
using Distribution.ModelSpace;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Distribution.DomainSpace
{
  public interface ICluster : IDisposable
  {
    /// <summary>
    /// Route for communication
    /// </summary>
    string Route { get; set; }

    /// <summary>
    /// Beacon service
    /// </summary>
    IBeacon Beacon { get; set; }

    /// <summary>
    /// Message processing communicator
    /// </summary>
    ICommunicator Communicator { get; set; }

    /// <summary>
    /// Instances
    /// </summary>
    ConcurrentDictionary<string, InstanceModel> Instances { get; }

    /// <summary>
    /// Get endpoint
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    InstanceModel GetInstance(string name);

    /// <summary>
    /// Send message
    /// </summary>
    /// <param name="name"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    Task<T> Send<T>(string name, dynamic message);
  }

  public class Cluster : ICluster
  {
    /// <summary>
    /// Route for communication
    /// </summary>
    public virtual string Route { get; set; }

    /// <summary>
    /// Beacon service
    /// </summary>
    public virtual IBeacon Beacon { get; set; }

    /// <summary>
    /// Message processing communicator
    /// </summary>
    public virtual ICommunicator Communicator { get; set; }

    /// <summary>
    /// Instances
    /// </summary>
    public virtual ConcurrentDictionary<string, InstanceModel> Instances { get; protected set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public Cluster()
    {
      Route = "/messages";
      Instances = new ConcurrentDictionary<string, InstanceModel>();
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public virtual void Dispose()
    {
      Beacon?.Dispose();
      Communicator?.Dispose();
      Instances.Clear();
    }

    /// <summary>
    /// Get endpoint
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public virtual InstanceModel GetInstance(string name)
    {
      if (Beacon.Instances.IsEmpty)
      {
        return default;
      }

      if (Instances.TryGetValue(name, out var instance) && Beacon.Instances.ContainsKey(instance.Address))
      {
        return instance;
      }

      var generator = new Random();
      var index = generator.Next(0, Beacon.Instances.Count - 1);
      var endpoint = Beacon.Instances.Values.ElementAt(index);

      return Instances[name] = endpoint;
    }

    /// <summary>
    /// Send message
    /// </summary>
    /// <param name="name"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public virtual Task<T> Send<T>(string name, dynamic message)
    {
      var instance = GetInstance(name);
      var source = new UriBuilder(null, instance.Address, Beacon.Port, Route);

      if (instance.Address is null)
      {
        return Task.FromResult<T>(default);
      }

      return Communicator.Send<T>($"{ source }", name, message, null, null);
    }
  }
}
