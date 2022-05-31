using Distribution.CommunicatorSpace;
using Distribution.ModelSpace;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
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
    ConcurrentDictionary<string, IBoxModel> Instances { get; }

    /// <summary>
    /// Get endpoint
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    IBoxModel GetInstance(string name);

    /// <summary>
    /// Send message
    /// </summary>
    /// <param name="name"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    Task<TResponse> Send<TResponse>(string name, object message);
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
    public virtual ConcurrentDictionary<string, IBoxModel> Instances { get; protected set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public Cluster()
    {
      Route = "/messages";
      Instances = new ConcurrentDictionary<string, IBoxModel>();
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public virtual void Dispose()
    {
      Instances.Clear();
      Beacon?.Dispose();
      Communicator?.Dispose();
    }

    /// <summary>
    /// Get endpoint
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public virtual IBoxModel GetInstance(string name)
    {
      if (Beacon.Points.IsEmpty)
      {
        return null;
      }

      if (Instances.TryGetValue(name, out IBoxModel source) && Beacon.Points.ContainsKey(source.Address))
      {
        return source;
      }

      var index = new Random().Next(0, Beacon.Points.Count - 1);
      var endpoint = Beacon.Points.Values.ElementAt(index);

      return Instances[name] = endpoint;
    }

    /// <summary>
    /// Send message
    /// </summary>
    /// <param name="name"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public virtual Task<TResponse> Send<TResponse>(string name, object message)
    {
      if (Beacon.Points.IsEmpty)
      {
        return Task.FromResult<TResponse>(default);
      }

      var address = GetInstance(name).Address;
      var source = new UriBuilder(null, address, Beacon.Port, Route);

      return Communicator.Send<TResponse>($"{ source }", name, message, null, null);
    }
  }
}
