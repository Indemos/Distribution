using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Distribution.Stream.Converters
{
  public class CustomConverter<T> : JsonConverter<T>
  {
    public override bool HandleNull => true;

    public override T Read(ref Utf8JsonReader reader, Type dataType, JsonSerializerOptions options)
    {
      try
      {
        return JsonSerializer.Deserialize<T>(ref reader, options);
      }
      catch (Exception)
      {
        return default;
      }
    }

    public override void Write(Utf8JsonWriter writer, T modelToWrite, JsonSerializerOptions options)
    {
      writer.WriteStringValue($"{modelToWrite}");
    }
  }
}
