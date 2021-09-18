using Distribution.DomainSpace;

namespace Distribution.CommunicatorSpace
{
  public interface ICommunicator : IDisposable
  {
    /// <summary>
    /// Timeout
    /// </summary>
    TimeSpan Timeout { get; set; }

    /// <summary>
    /// Connect
    /// </summary>
    /// <returns></returns>
    Task Connect();

    /// <summary>
    /// Disconnect
    /// </summary>
    /// <returns></returns>
    Task Disconnect();

    /// <summary>
    /// Send message
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    Task Send(dynamic message);

    /// <summary>
    /// Subscribe to messages
    /// </summary>
    /// <param name="app"></param>
    /// <param name="actorSystem"></param>
    /// <param name="route"></param>
    /// <returns></returns>
    Task Subscribe(IApplicationBuilder app, IScene actorSystem, string route);
  }

  public abstract class Communicator : ICommunicator
  {
    /// <summary>
    /// Timeout
    /// </summary>
    public virtual TimeSpan Timeout { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public Communicator()
    {
      Timeout = TimeSpan.FromSeconds(15);
    }

    /// <summary>
    /// Connect
    /// </summary>
    /// <returns></returns>
    public virtual Task Connect()
    {
      return Task.CompletedTask;
    }

    /// <summary>
    /// Disconnect
    /// </summary>
    /// <returns></returns>
    public virtual Task Disconnect()
    {
      return Task.CompletedTask;
    }

    /// <summary>
    /// Send message
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public virtual Task Send(dynamic message)
    {
      return Task.CompletedTask;
    }

    /// <summary>
    /// Subscribe to messages
    /// </summary>
    /// <param name="app"></param>
    /// <param name="actorSystem"></param>
    /// <param name="route"></param>
    /// <returns></returns>
    public virtual Task Subscribe(IApplicationBuilder app, IScene actorSystem, string route)
    {
      return Task.CompletedTask;
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public virtual void Dispose()
    {
    }
  }
}