using System.Collections;
using System.Text.Json;
using System.Web;
using Distribution.DomainSpace;

namespace Distribution.CommunicatorSpace
{
  public class RouteCommunicator : Communicator, ICommunicator
  {
    protected HttpClient _client = null;

    /// <summary>
    /// Constructor
    /// </summary>
    public RouteCommunicator()
    {
      if (_client == null)
      {
        var socket = new SocketsHttpHandler
        {
          MaxConnectionsPerServer = 1000,
          PooledConnectionLifetime = TimeSpan.FromMinutes(10),
          PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
        };

        _client = new HttpClient(socket);
      }
    }

    /// <summary>
    /// Deserialize
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="input"></param>
    /// <returns></returns>
    public object Decode(string input)
    {
      if (string.IsNullOrEmpty(input))
      {
        return default;
      }

      return JsonSerializer.Deserialize(input, typeof(object));
    }

    /// <summary>
    /// Deserialize
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="input"></param>
    /// <returns></returns>
    public object Decode(Stream input)
    {
      if (input == null)
      {
        return null;
      }

      return JsonSerializer.Deserialize(input, typeof(object));
    }

    /// <summary>
    /// Deserialize
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public string Encode(dynamic input)
    {
      if (input == null)
      {
        return null;
      }

      return JsonSerializer.Serialize(input);
    }

    /// <summary>
    /// Intercept incoming HTTP queries
    /// </summary>
    /// <param name="app"></param>
    /// <param name="actorSystem"></param>
    /// <param name="route"></param>
    public override Task Subscribe(IApplicationBuilder app, IScene actorSystem, string route)
    {
      app.Run(async context =>
      {
        if (string.Equals(context.Request.Method, "OPTIONS"))
        {
          await context.Response.CompleteAsync();
          return;
        }

        if (context.Request.Path.Value.Contains(route))
        {
          var session = context.Session.Id;
          var envelope = context.Request.Body;
          var content = await GetInput(envelope);
          var values = actorSystem.Send(session, "SomeActor", content).Select(async o => await o);

          //await context.Response.WriteAsync(Encode(null));
        }

        await context.Response.CompleteAsync();
      });

      return Task.CompletedTask;
    }

    /// <summary>
    /// Get input params from the HTTP query
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public async Task<object> GetInput(Stream stream)
    {
      if (stream.CanSeek)
      {
        stream.Seek(0, SeekOrigin.Begin);
      }

      using (var streamReader = new StreamReader(stream))
      {
        var input = await streamReader.ReadToEndAsync();
        return Decode(input);
      }
    }

    /// <summary>
    /// Send GET request
    /// </summary>
    /// <param name="source"></param>
    /// <param name="inputs"></param>
    /// <param name="options"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    public Task<Stream> Send(
      string source,
      IDictionary<dynamic, dynamic> inputs = null,
      IDictionary<dynamic, dynamic> options = null,
      CancellationTokenSource cts = null)
    {
      return SendData(HttpMethod.Get, source, inputs, options, null, cts);
    }

    /// <summary>
    /// Send POST request
    /// </summary>
    /// <param name="source"></param>
    /// <param name="inputs"></param>
    /// <param name="options"></param>
    /// <param name="content"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    public Task<Stream> Send(
      string source,
      IDictionary<dynamic, dynamic> inputs = null,
      IDictionary<dynamic, dynamic> options = null,
      HttpContent content = null,
      CancellationTokenSource cts = null)
    {
      return SendData(HttpMethod.Post, source, inputs, options, content, cts);
    }

    /// <summary>
    /// Stream HTTP content
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="inputs"></param>
    /// <param name="headers"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    public async Task<Stream> Stream(
      string source,
      IDictionary<dynamic, dynamic> inputs = null,
      IDictionary<dynamic, dynamic> headers = null,
      CancellationTokenSource cts = null)
    {
      using (var client = new HttpClient())
      {
        var cancellation = cts == null ? CancellationToken.None : cts.Token;

        if (headers is IEnumerable)
        {
          foreach (var item in headers)
          {
            client.DefaultRequestHeaders.Add($"{ item.Key }", $"{ item.Value }");
          }
        }

        return await client
          .GetStreamAsync(source + "?" + GetQuery(inputs), cancellation)
          .ConfigureAwait(false);
      }
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public override void Dispose()
    {
      _client.Dispose();
    }

    /// <summary>
    /// Generic query sender
    /// </summary>
    /// <param name="queryType"></param>
    /// <param name="source"></param>
    /// <param name="inputs"></param>
    /// <param name="content"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    protected async Task<Stream> SendData(
      HttpMethod queryType,
      string source,
      IDictionary<dynamic, dynamic> inputs = null,
      IDictionary<dynamic, dynamic> headers = null,
      HttpContent content = null,
      CancellationTokenSource cts = null)
    {
      var message = new HttpRequestMessage
      {
        Content = content,
        Method = queryType,
        RequestUri = new Uri(source + "?" + GetQuery(inputs))
      };

      if (headers is IEnumerable)
      {
        foreach (var item in headers)
        {
          message.Headers.Add($"{ item.Key }", $"{ item.Value }");
        }
      }

      if (cts == null)
      {
        cts = new CancellationTokenSource(Timeout);
      }

      HttpResponseMessage response = null;

      try
      {
        response = await _client
          .SendAsync(message, cts.Token)
          .ConfigureAwait(false);
      }
      catch (Exception)
      {
        return null;
      }

      return await response
        .Content
        .ReadAsStreamAsync(cts.Token)
        .ConfigureAwait(false);
    }

    /// <summary>
    /// Convert dictionary to URL params
    /// </summary>
    /// <param name="inputs"></param>
    /// <returns></returns>
    protected string GetQuery(IDictionary<object, object> query)
    {
      var inputs = HttpUtility.ParseQueryString(string.Empty);

      if (query is IEnumerable)
      {
        foreach (var item in query)
        {
          inputs.Add($"{ item.Key }", $"{ item.Value }");
        }
      }

      return $"{ inputs }";
    }
  }
}