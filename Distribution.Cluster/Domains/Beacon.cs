using Distribution.Models;
using Distribution.Services;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;

namespace Distribution.Cluster.Domains
{
  public interface IBeacon : IDisposable
  {
    /// <summary>
    /// Port
    /// </summary>
    int Port { get; set; }

    /// <summary>
    /// Endpoint deletion stream
    /// </summary>
    Action<InstanceModel> DropStream { get; }

    /// <summary>
    /// Endpoint creation stream
    /// </summary>
    Action<InstanceModel> CreateStream { get; }

    /// <summary>
    /// Endpoints
    /// </summary>
    ConcurrentDictionary<string, InstanceModel> Instances { get; }

    /// <summary>
    /// Clear unresponsive nodes
    /// </summary>
    /// <param name="dropTime"></param>
    /// <returns></returns>
    void Drop(TimeSpan dropTime);

    /// <summary>
    /// Send message
    /// </summary>
    /// <param name="message"></param>
    /// <param name="port"></param>
    /// <returns></returns>
    int Send(string message, int port);

    /// <summary>
    /// Subscribe to messages
    /// </summary>
    /// <param name="name"></param>
    /// <param name="port"></param>
    /// <param name="createSpan"></param>
    /// <param name="dropSpan"></param>
    /// <returns></returns>
    IDisposable Locate(string name, int port, TimeSpan createSpan, TimeSpan dropSpan);

    /// <summary>
    /// Create node reference
    /// </summary>
    /// <param name="endpoint"></param>
    /// <returns></returns>
    InstanceModel CreateInstance(IPEndPoint endpoint);
  }

  public class Beacon : IBeacon
  {
    /// <summary>
    /// Port
    /// </summary>
    public virtual int Port { get; set; }

    /// <summary>
    /// UDP client
    /// </summary>
    public virtual UdpClient Communicator { get; set; }

    /// <summary>
    /// Endpoint dropping stream
    /// </summary>
    public virtual Action<InstanceModel> DropStream { get; set; }

    /// <summary>
    /// Endpoint creation stream
    /// </summary>
    public virtual Action<InstanceModel> CreateStream { get; set; }

    /// <summary>
    /// Endpoints
    /// </summary>
    public virtual ConcurrentDictionary<string, InstanceModel> Instances { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public Beacon()
    {
      Port = 2000;
      DropStream = o => { };
      CreateStream = o => { };
      Instances = new ConcurrentDictionary<string, InstanceModel>();
      Communicator = new UdpClient(AddressFamily.InterNetwork)
      {
        EnableBroadcast = true,
        ExclusiveAddressUse = false
      };
    }

    /// <summary>
    /// Create node reference
    /// </summary>
    /// <param name="endpoint"></param>
    /// <returns></returns>
    public virtual InstanceModel CreateInstance(IPEndPoint endpoint)
    {
      return new InstanceModel
      {
        Time = DateTime.UtcNow,
        Address = $"{endpoint.Address}"
      };
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public virtual void Dispose()
    {
      Instances?.Clear();
      Communicator?.Dispose();
    }

    /// <summary>
    /// Send message
    /// </summary>
    /// <param name="message"></param>
    /// <param name="port"></param>
    /// <returns></returns>
    public virtual int Send(string message, int port)
    {
      var data = Encoding.UTF8.GetBytes(message);
      var endpoint = new IPEndPoint(IPAddress.Broadcast, port);

      return Communicator.Send(data, data.Length, endpoint);
    }

    /// <summary>
    /// Subscribe to messages
    /// </summary>
    /// <param name="name"></param>
    /// <param name="port"></param>
    /// <param name="createSpan"></param>
    /// <param name="dropSpan"></param>
    /// <returns></returns>
    public virtual IDisposable Locate(string name, int port, TimeSpan createSpan, TimeSpan dropSpan)
    {
      var endpoint = new IPEndPoint(IPAddress.Any, port);

      Communicator.Client.Bind(endpoint);

      var interval = new Timer(TimeSpan.FromSeconds(1));
      var scheduler = InstanceService<ScheduleService>.Instance;

      interval.Enabled = true;
      interval.Elapsed += (sender, e) => scheduler.Send(() =>
      {
        Drop(dropSpan);
        Communicator.ReceiveAsync().ContinueWith(async o =>
        {
          var response = await o;
          var instance = response.RemoteEndPoint;
          var message = Encoding.UTF8.GetString(response.Buffer);

          if (Equals(name, message))
          {
            if (Instances.TryGetValue($"{endpoint.Address}", out var item) is false)
            {
              CreateStream(item = Instances[$"{instance.Address}"] = CreateInstance(instance));
            }

            item.Time = DateTime.UtcNow;
          }
        });
      });

      return interval;
    }

    /// <summary>
    /// Clear unresponsive nodes
    /// </summary>
    /// <param name="dropTime"></param>
    /// <returns></returns>
    public virtual void Drop(TimeSpan dropTime)
    {
      Instances = new ConcurrentDictionary<string, InstanceModel>(Instances.Where(item =>
      {
        if (DateTime.UtcNow.Ticks - item.Value.Time.Value.Ticks > dropTime.Ticks)
        {
          DropStream(item.Value);
          return false;
        }

        return true;
      }));
    }
  }
}
