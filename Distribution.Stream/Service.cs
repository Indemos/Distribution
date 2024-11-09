using Distribution.Stream.Models;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Distribution.Stream
{
  public class Service : IDisposable
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
    /// Serialization options
    /// </summary>
    public virtual JsonSerializerOptions Options { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public Service() : this(new HttpClient())
    {
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public Service(HttpClient client)
    {
      Client = client;
      Timeout = TimeSpan.FromSeconds(15);
      Options = new JsonSerializerOptions
      {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        ReadCommentHandling = JsonCommentHandling.Skip,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling =
          JsonNumberHandling.AllowReadingFromString |
          JsonNumberHandling.AllowNamedFloatingPointLiterals |
          JsonNumberHandling.WriteAsString,
        Converters =
        {
          new Converters.CustomConverter<bool>(),
          new Converters.CustomConverter<byte>(),
          new Converters.CustomConverter<sbyte>(),
          new Converters.CustomConverter<short>(),
          new Converters.CustomConverter<ushort>(),
          new Converters.CustomConverter<int>(),
          new Converters.CustomConverter<uint>(),
          new Converters.CustomConverter<long>(),
          new Converters.CustomConverter<ulong>(),
          new Converters.CustomConverter<float>(),
          new Converters.CustomConverter<double>(),
          new Converters.CustomConverter<decimal>(),
          new Converters.CustomConverter<char>(),
          new Converters.CustomConverter<string>(),
          new Converters.CustomConverter<DateOnly>(),
          new Converters.CustomConverter<TimeOnly>(),
          new Converters.CustomConverter<DateTime>()
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
    public virtual async Task<System.IO.Stream> Stream(HttpRequestMessage message, CancellationTokenSource cts = null)
    {
      cts ??= new CancellationTokenSource(Timeout);

      using (var client = new HttpClient())
      {
        return await client
          .GetStreamAsync(message.RequestUri, cts.Token)
          .ConfigureAwait(false);
      }
    }

    /// <summary>
    /// Generic query sender
    /// </summary>
    /// <param name="message"></param>
    /// <param name="options"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    public virtual async Task<ResponseModel<T>> Send<T>(
      HttpRequestMessage message,
      JsonSerializerOptions options = null,
      CancellationTokenSource cts = null)
    {
      var response = new ResponseModel<T>();

      try
      {
        cts ??= new CancellationTokenSource(Timeout);

        using (var res = await Client.SendAsync(message, cts.Token).ConfigureAwait(false))
        using (var content = await res.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false))
        {
          response.Message = res;

          if (res.IsSuccessStatusCode is false)
          {
            response.Error = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
            return response;
          }

          response.Data = await JsonSerializer.DeserializeAsync<T>(content, options).ConfigureAwait(false);
        }
      }
      catch (Exception e)
      {
        response.Error = e.Message;
      }

      return response;
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public virtual void Dispose() => Client?.Dispose();
  }
}
