using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using Distribution.ModelSpace;

namespace Distribution.DomainSpace
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
    ISubject<IBoxModel> DropStream { get; }

    /// <summary>
    /// Endpoint creation stream
    /// </summary>
    ISubject<IBoxModel> CreateStream { get; }

    /// <summary>
    /// Endpoints
    /// </summary>
    ConcurrentDictionary<string, IBoxModel> Boxes { get; }

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
    /// Get node reference
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    IBoxModel GetBox(IPEndPoint endpoint);

    /// <summary>
    /// Create node reference
    /// </summary>
    /// <param name="endpoint"></param>
    /// <returns></returns>
    IBoxModel CreateBox(IPEndPoint endpoint);
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
    public virtual UdpClient Communicator { get; protected set; }

    /// <summary>
    /// Endpoint dropping stream
    /// </summary>
    public virtual ISubject<IBoxModel> DropStream { get; protected set; }

    /// <summary>
    /// Endpoint creation stream
    /// </summary>
    public virtual ISubject<IBoxModel> CreateStream { get; protected set; }

    /// <summary>
    /// Endpoints
    /// </summary>
    public virtual ConcurrentDictionary<string, IBoxModel> Boxes { get; protected set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public Beacon()
    {
      Port = 2000;
      DropStream = new Subject<IBoxModel>();
      CreateStream = new Subject<IBoxModel>();
      Boxes = new ConcurrentDictionary<string, IBoxModel>();
      Communicator = new UdpClient(AddressFamily.InterNetwork)
      {
        EnableBroadcast = true,
        ExclusiveAddressUse = false
      };
    }

    /// <summary>
    /// Get node reference
    /// </summary>
    /// <param name="endpoint"></param>
    /// <returns></returns>
    public virtual IBoxModel GetBox(IPEndPoint endpoint)
    {
      Boxes.TryGetValue($"{ endpoint.Address }", out IBoxModel response);
      return response;
    }

    /// <summary>
    /// Create node reference
    /// </summary>
    /// <param name="endpoint"></param>
    /// <returns></returns>
    public virtual IBoxModel CreateBox(IPEndPoint endpoint)
    {
      return new BoxModel
      {
        Time = DateTime.UtcNow,
        Address = $"{ endpoint.Address }"
      };
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public virtual void Dispose()
    {
      Boxes?.Clear();
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

      return Observable
        .Interval(createSpan, new EventLoopScheduler())
        .Subscribe(o =>
        {
          Drop(dropSpan);
          Communicator.ReceiveAsync().ContinueWith(async o =>
          {
            var response = await o;
            var box = response.RemoteEndPoint;
            var message = Encoding.UTF8.GetString(response.Buffer);

            if (Equals(name, message))
            {
              var item = GetBox(box);

              if (item is null)
              {
                CreateStream.OnNext(item = Boxes[$"{box.Address}"] = CreateBox(box));
              }

              item.Time = DateTime.UtcNow;
            }
          });
        });
    }

    /// <summary>
    /// Clear unresponsive nodes
    /// </summary>
    /// <param name="dropTime"></param>
    /// <returns></returns>
    public virtual void Drop(TimeSpan dropTime)
    {
      Boxes = new ConcurrentDictionary<string, IBoxModel>(Boxes.Where(item =>
      {
        if (DateTime.UtcNow.Ticks - item.Value.Time.Value.Ticks > dropTime.Ticks)
        {
          DropStream.OnNext(item.Value);
          return false;
        }

        return true;
      }));
    }
  }
}
