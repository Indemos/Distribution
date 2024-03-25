using Distribution.Domains;
using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Distribution.Cluster.Communicators
{
  public interface ICommunicator : IDisposable
  {
    /// <summary>
    /// Timeout
    /// </summary>
    TimeSpan Timeout { get; set; }

    /// <summary>
    /// Scene
    /// </summary>
    IScene Scene { get; set; }

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
    /// <param name="source"></param>
    /// <param name="name"></param>
    /// <param name="inputs"></param>
    /// <param name="options"></param>
    /// <param name="message"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    Task<T> Send<T>(
      string source,
      string name,
      object message,
      IDictionary<object, object> inputs = null,
      IDictionary<object, object> options = null,
      CancellationTokenSource cts = null);

    /// <summary>
    /// Create stream
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="inputs"></param>
    /// <param name="options"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    Task<Stream> Stream(
      string source,
      IDictionary<object, object> inputs = null,
      IDictionary<object, object> options = null,
      CancellationTokenSource cts = null);

    /// <summary>
    /// Intercept incoming HTTP queries
    /// </summary>
    /// <param name="app"></param>
    /// <param name="route"></param>
    Task Subscribe(IApplicationBuilder app, string route);
  }

  public abstract class Communicator : ICommunicator
  {
    /// <summary>
    /// Timeout
    /// </summary>
    public virtual TimeSpan Timeout { get; set; }

    /// <summary>
    /// Scene
    /// </summary>
    public virtual IScene Scene { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public Communicator()
    {
      Timeout = TimeSpan.FromSeconds(15);
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public virtual void Dispose() => Disconnect();

    /// <summary>
    /// Connect
    /// </summary>
    /// <returns></returns>
    public virtual Task Connect() => Task.FromResult(0);

    /// <summary>
    /// Disconnect
    /// </summary>
    /// <returns></returns>
    public virtual Task Disconnect() => Task.FromResult(0);

    /// <summary>
    /// Send message
    /// </summary>
    /// <param name="source"></param>
    /// <param name="name"></param>
    /// <param name="inputs"></param>
    /// <param name="options"></param>
    /// <param name="message"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    public abstract Task<T> Send<T>(
      string source,
      string name,
      object message,
      IDictionary<object, object> inputs = null,
      IDictionary<object, object> options = null,
      CancellationTokenSource cts = null);

    /// <summary>
    /// Create stream
    /// </summary>
    /// <param name="source"></param>
    /// <param name="inputs"></param>
    /// <param name="options"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    public abstract Task<Stream> Stream(
      string source,
      IDictionary<object, object> inputs = null,
      IDictionary<object, object> options = null,
      CancellationTokenSource cts = null);

    /// <summary>
    /// Subscribe to messages
    /// </summary>
    /// <param name="app"></param>
    /// <param name="route"></param>
    /// <returns></returns>
    public abstract Task Subscribe(IApplicationBuilder app, string route);
  }
}
