using System.Collections;
using System.Dynamic;
using System.Text.Json;
using System.Web;
using Distribution.DomainSpace;
using Distribution.ModelSpace;

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
      var socket = new SocketsHttpHandler
      {
        MaxConnectionsPerServer = 1000,
        PooledConnectionLifetime = TimeSpan.FromMinutes(10),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
      };

      _client = new HttpClient(socket);
    }

    /// <summary>
    /// Send POST request
    /// </summary>
    /// <param name="source"></param>
    /// <param name="name"></param>
    /// <param name="message"></param>
    /// <param name="inputs"></param>
    /// <param name="options"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    public override async Task<TResponse> Send<TResponse>(
      string source,
      string name,
      object message,
      IDictionary<object, object> inputs = null,
      IDictionary<object, object> options = null,
      CancellationTokenSource cts = null)
    {
      var code = Encode(new MessageModel
      {
        Name = name,
        Message = message,
        Descriptor = message.GetType().FullName
      });

      var content = new StringContent(code);
      var response = await SendData(HttpMethod.Post, source, inputs, options, content, cts);

      return await Decode<TResponse>(response);
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
    public override async Task<Stream> Stream(
      string source,
      IDictionary<object, object> inputs = null,
      IDictionary<object, object> headers = null,
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
          .GetStreamAsync(new UriBuilder(source + "?" + GetQuery(inputs)).Uri, cancellation)
          .ConfigureAwait(false);
      }
    }

    /// <summary>
    /// Intercept incoming HTTP queries
    /// </summary>
    /// <param name="app"></param>
    /// <param name="route"></param>
    public override Task Subscribe(IApplicationBuilder app, string route)
    {
      app.Run(async context =>
      {
        if (string.Equals(context.Request.Method, "OPTIONS"))
        {
          await context.Response.CompleteAsync();
          return;
        }

        if (context.Request.Path.Value.StartsWith(route))
        {
          var session = context.Session.Id;
          var content = context.Request.Body;
          var envelope = await Decode<MessageModel>(content);

          if (envelope is not null)
          {
            var messageType = Scene.Messages[envelope.Descriptor];
            var message = Decode($"{ envelope.Message }", messageType);
            var response = await Scene.Send($"{ session }:{ envelope.Name }", message);

            await context.Response.WriteAsync(Encode(response as object));
          }
        }

        await context.Response.CompleteAsync();
      });

      return Task.FromResult(0);
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public override void Dispose()
    {
      _client.Dispose();
    }

    /// <summary>
    /// Deserialize
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    protected virtual string Encode(object input)
    {
      if (input == null)
      {
        return null;
      }

      return JsonSerializer.Serialize(input);
    }

    /// <summary>
    /// Deserialize
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="input"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    protected virtual object Decode(string input, Type name)
    {
      if (input == null)
      {
        return default;
      }

      return JsonSerializer.Deserialize(input, name);
    }

    /// <summary>
    /// Deserialize
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="input"></param>
    /// <returns></returns>
    protected virtual ValueTask<T> Decode<T>(Stream input)
    {
      if (input == null)
      {
        return default;
      }

      return JsonSerializer.DeserializeAsync<T>(input);
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
      IDictionary<object, object> inputs = null,
      IDictionary<object, object> headers = null,
      HttpContent content = null,
      CancellationTokenSource cts = null)
    {
      var message = new HttpRequestMessage
      {
        Content = content,
        Method = queryType,
        RequestUri = new UriBuilder(source + "?" + GetQuery(inputs)).Uri
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
