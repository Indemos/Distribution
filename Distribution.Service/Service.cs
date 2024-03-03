using Distribution.Service.Models;
using Distribution.ServiceSpace;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Distribution.Service
{
  public class Service
  {
    /// <summary>
    /// Max execution time
    /// </summary>
    public virtual TimeSpan Timeout { get; set; }

    /// <summary>
    /// HTTP client instance
    /// </summary>
    public virtual HttpClient Client { get; set; }

    /// <summary>
    /// Scheduler
    /// </summary>
    public virtual ScheduleService Scheduler { get; set; }

    /// <summary>
    /// Serialization options
    /// </summary>
    public virtual JsonSerializerOptions Options { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="client"></param>
    public Service(HttpClient client = null)
    {
      Client = client ?? new HttpClient();
      Timeout = TimeSpan.FromSeconds(15);
      Scheduler = new ScheduleService();
      Options = new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Converters =
        {
          new Converters.DateConverter(),
          new Converters.DoubleConverter()
        },
        TypeInfoResolver = GetResolver()
      };
    }

    /// <summary>
    /// Create more resolver with more permissive naming policy for better matching
    /// </summary>
    /// <returns></returns>
    public virtual DefaultJsonTypeInfoResolver GetResolver() => new()
    {
      Modifiers =
      {
        contract => contract.Properties.ToList().ForEach(property =>
        {
          var name = Regex.Replace(property.Name,"(.)([A-Z])","$1_$2");

          if (string.Equals(name, property.Name))
          {
            return;
          }

          var o = contract.CreateJsonPropertyInfo(property.PropertyType, name);

          o.Set = property.Set;
          o.AttributeProvider = property.AttributeProvider;

          contract.Properties.Add(o);
        })
      }
    };

    /// <summary>
    /// Stream HTTP content
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="message"></param>
    /// <param name="options"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    public virtual async Task<Stream> Stream(
      HttpRequestMessage message,
      JsonSerializerOptions options = null,
      CancellationTokenSource cts = null)
    {
      using (var client = new HttpClient())
      {
        return await client.GetStreamAsync(message.RequestUri, (cts ?? new CancellationTokenSource(Timeout)).Token);
      }
    }

    /// <summary>
    /// Generic query sender
    /// </summary>
    /// <param name="message"></param>
    /// <param name="options"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    public virtual Task<ResponseModel<T>> Send<T>(
      HttpRequestMessage message,
      JsonSerializerOptions options = null,
      CancellationTokenSource cts = null)
    {
      var response = new ResponseModel<T>();
      var completion = new TaskCompletionSource<ResponseModel<T>>(TaskCreationOptions.RunContinuationsAsynchronously);

      try
      {
        cts ??= new CancellationTokenSource(Timeout);

        Scheduler.Send(async () =>
        {
          try
          {
            var res = await Client.SendAsync(message, cts.Token);
            var content = await res.Content.ReadAsStreamAsync(cts.Token);
            var entity = await JsonSerializer.DeserializeAsync<T>(content, Options);

            completion.TrySetResult(new ResponseModel<T>
            {
              Data = entity
            });
          }
          catch (Exception e)
          {
            completion.TrySetException(e);
          }
        });
      }
      catch (Exception e)
      {
        response.Error = e.Message;
      }

      return completion.Task;
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public virtual void Dispose()
    {
      Client?.Dispose();
      Scheduler?.Dispose();
    }
  }
}
