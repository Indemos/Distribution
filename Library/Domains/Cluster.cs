using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using Distribution.ModelSpace;

namespace Distribution.DomainSpace
{
  public interface ICluster : IDisposable
  {
    /// <summary>
    /// Port
    /// </summary>
    int Port { get; set; }

    /// <summary>
    /// Message to broadcast
    /// </summary>
    string Message { get; set; }

    /// <summary>
    /// How often to check that host is up
    /// </summary>
    TimeSpan ClearSpan { get; set; }

    /// <summary>
    /// How often to send messages
    /// </summary>
    TimeSpan SendSpan { get; set; }

    /// <summary>
    /// How often to check incoming messages
    /// </summary>
    TimeSpan SubscribeSpan { get; set; }

    /// <summary>
    /// Drop time after which the host is considered dead
    /// </summary>
    TimeSpan DeleteSpan { get; set; }

    /// <summary>
    /// Node creation stream
    /// </summary>
    IObservable<IPointModel> CreateStream { get; }

    /// <summary>
    /// Node deletion stream
    /// </summary>
    IObservable<IPointModel> DeleteStream { get; }

    /// <summary>
    /// Clear dead nodes
    /// </summary>
    /// <param name="dropTime"></param>
    /// <returns></returns>
    int Clear(TimeSpan dropTime);

    /// <summary>
    /// Clean dead nodes on timer
    /// </summary>
    /// <returns></returns>
    int ClearInterval();

    /// <summary>
    /// Send message
    /// </summary>
    /// <param name="port"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    int Send(int port, string message);

    /// <summary>
    /// Subscribe to messages
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    int Subscribe(string message);

    /// <summary>
    /// Send message on timer
    /// </summary>
    /// <returns></returns>
    int SendInterval();

    /// <summary>
    /// Get node reference
    /// </summary>
    /// <param name="itemName"></param>
    /// <returns></returns>
    IPointModel GetItem(string itemName);

    /// <summary>
    /// Create node reference
    /// </summary>
    /// <param name="itemName"></param>
    /// <param name="endpoint"></param>
    /// <returns></returns>
    IPointModel CreateItem(string itemName, IPEndPoint endpoint);
  }

  public class Cluster : ICluster
  {
    protected UdpClient _client = null;
    protected IDisposable _creator = null;
    protected IDisposable _cleaner = null;
    protected IDisposable _producer = null;
    protected EventLoopScheduler _sendScheduler = null;
    protected EventLoopScheduler _clearScheduler = null;
    protected EventLoopScheduler _subscribeScheduler = null;
    protected ISubject<IPointModel> _createStream = null;
    protected ISubject<IPointModel> _deleteStream = null;
    protected ConcurrentDictionary<string, IPointModel> _items = new();

    /// <summary>
    /// Port
    /// </summary>
    public virtual int Port { get; set; }

    /// <summary>
    /// Message to broadcast
    /// </summary>
    public virtual string Message { get; set; }

    /// <summary>
    /// How often to check that host is up
    /// </summary>
    public virtual TimeSpan ClearSpan { get; set; }

    /// <summary>
    /// How often to send messages
    /// </summary>
    public virtual TimeSpan SendSpan { get; set; }

    /// <summary>
    /// How often to check incoming messages
    /// </summary>
    public virtual TimeSpan SubscribeSpan { get; set; }

    /// <summary>
    /// Drop time after which the host is considered dead
    /// </summary>
    public virtual TimeSpan DeleteSpan { get; set; }

    /// <summary>
    /// Node creation stream
    /// </summary>
    public virtual IObservable<IPointModel> CreateStream => _createStream.AsObservable();

    /// <summary>
    /// Node deletion stream
    /// </summary>
    public virtual IObservable<IPointModel> DeleteStream => _deleteStream.AsObservable();

    /// <summary>
    /// Constructor
    /// </summary>
    public Cluster()
    {
      Port = 2000;
      SendSpan = TimeSpan.FromSeconds(1);
      ClearSpan = TimeSpan.FromSeconds(5);
      DeleteSpan = TimeSpan.FromSeconds(10);
      SubscribeSpan = TimeSpan.FromMilliseconds(1);

      _sendScheduler = new EventLoopScheduler();
      _clearScheduler = new EventLoopScheduler();
      _subscribeScheduler = new EventLoopScheduler();
      _createStream = new Subject<IPointModel>();
      _deleteStream = new Subject<IPointModel>();
      _client = new UdpClient(AddressFamily.InterNetwork)
      {
        EnableBroadcast = true
      };
    }

    /// <summary>
    /// Get node reference
    /// </summary>
    /// <param name="itemName"></param>
    /// <returns></returns>
    public virtual IPointModel GetItem(string itemName)
    {
      if (_items.TryGetValue(itemName, out IPointModel response))
      {
        return response;
      }

      return null;
    }

    /// <summary>
    /// Create node reference
    /// </summary>
    /// <param name="itemName"></param>
    /// <param name="endpoint"></param>
    /// <returns></returns>
    public virtual IPointModel CreateItem(string itemName, IPEndPoint endpoint)
    {
      return _items[itemName] = new PointModel
      {
        Port = endpoint.Port,
        Time = DateTime.UtcNow,
        Name = Dns.GetHostEntry(endpoint.Address).HostName,
        Address = $"{ endpoint.Address }"
      };
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public virtual void Dispose()
    {
      _client?.Dispose();
      _creator?.Dispose();
      _cleaner?.Dispose();
      _producer?.Dispose();
      _sendScheduler?.Dispose();
      _clearScheduler?.Dispose();
      _subscribeScheduler?.Dispose();
    }

    /// <summary>
    /// Send message
    /// </summary>
    /// <param name="port"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public virtual int Send(int port, string message)
    {
      var data = Encoding.UTF8.GetBytes(message);
      var endpoint = new IPEndPoint(IPAddress.Broadcast, port);

      return _client.Send(data, data.Length, endpoint);
    }

    /// <summary>
    /// Send message on timer
    /// </summary>
    /// <param name="port"></param>
    /// <param name="interval"></param>
    /// <returns></returns>
    public virtual int SendInterval()
    {
      _producer?.Dispose();
      _producer = Observable
        .Interval(SendSpan, _sendScheduler)
        .Subscribe(o => Send(Port, Message));

      return 0;
    }

    /// <summary>
    /// Subscribe to messages
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public virtual int Subscribe(string message)
    {
      var endpoint = new IPEndPoint(IPAddress.Any, Port);

      _client.Client.Bind(endpoint);

      _creator?.Dispose();
      _creator = Observable
        .Interval(SubscribeSpan, _subscribeScheduler)
        .Subscribe(o =>
        {
          try
          {
            var message = Encoding.UTF8.GetString(_client.Receive(ref endpoint));
            var address = $"{ endpoint.Address }";
            var item = GetItem(address);

            if (item == null)
            {
              _createStream.OnNext(item = CreateItem(address, endpoint));
            }

            item.Time = DateTime.UtcNow;
          }
          catch (Exception) {}
        });

      return 0;
    }

    /// <summary>
    /// Clear dead nodes on timer
    /// </summary>
    /// <returns></returns>
    public virtual int ClearInterval()
    {
      _cleaner?.Dispose();
      _cleaner = Observable
        .Interval(ClearSpan, _clearScheduler)
        .Subscribe(o => Clear(DeleteSpan));

      return 0;
    }

    /// <summary>
    /// Clear dead nodes
    /// </summary>
    /// <param name="dropTime"></param>
    /// <returns></returns>
    public virtual int Clear(TimeSpan dropTime)
    {
      _items = new ConcurrentDictionary<string, IPointModel>(_items.Where(item =>
      {
        var isDead = item.Value.Time.Value.Ticks + dropTime.Ticks < DateTime.UtcNow.Ticks;

        if (isDead)
        {
          _items.TryRemove(item);
          _deleteStream.OnNext(item.Value);
        }

        return isDead;
      }));

      return 0;
    }
  }
}