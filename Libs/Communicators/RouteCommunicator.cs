using Distribution.ModelSpace;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

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
    public override async Task<T> Send<T>(
      string source,
      string name,
      object message,
      IDictionary<object, object> inputs = null,
      IDictionary<object, object> options = null,
      CancellationTokenSource cts = null)
    {
      var code = Encode(new EnvelopeModel
      {
        Name = name,
        Message = message,
        Descriptor = message.GetType().FullName
      });

      var content = new StringContent(code);
      var response = await SendData(HttpMethod.Post, source, inputs, options, content, cts);

      return await Decode<T>(response);
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
        var cancellation = cts is null ? CancellationToken.None : cts.Token;

        if (headers is not null)
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
          var envelope = await Decode<EnvelopeModel>(content);

          if (envelope.Descriptor is not null)
          {
            var descriptor = Scene.GetMessage(envelope.Descriptor);
            var message = Decode($"{ envelope.Message }", descriptor);
            var response = await Scene.Send<object>($"{ session }:{ envelope.Name }", message);

            await context.Response.WriteAsync(Encode(response));
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
      if (input is null)
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
      if (input is null)
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
      if (input is null)
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

      if (headers is not null)
      {
        foreach (var item in headers)
        {
          message.Headers.Add($"{ item.Key }", $"{ item.Value }");
        }
      }

      if (cts is null)
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

      if (query is not null)
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
