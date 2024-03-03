using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Distribution.Service.Converters
{
  public class LongConverter : JsonConverter<long>
  {
    public override bool HandleNull => true;

    public override long Read(ref Utf8JsonReader reader, Type dataType, JsonSerializerOptions options)
    {
      if (reader.TokenType is JsonTokenType.Number && reader.TryGetInt64(out var response))
      {
        return response;
      }

      return default;
    }

    public override void Write(
      Utf8JsonWriter writer,
      long modelToWrite,
      JsonSerializerOptions options) =>
      JsonSerializer.Serialize(writer, modelToWrite, modelToWrite.GetType(), options);
  }
}
