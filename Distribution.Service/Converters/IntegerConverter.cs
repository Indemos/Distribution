using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Distribution.Stream.Converters
{
  public class IntegerConverter : JsonConverter<int>
  {
    public override bool HandleNull => true;

    public override int Read(ref Utf8JsonReader reader, Type dataType, JsonSerializerOptions options)
    {
      if (reader.TokenType is JsonTokenType.Number && reader.TryGetInt32(out var response))
      {
        return response;
      }

      return default;
    }

    public override void Write(
      Utf8JsonWriter writer,
      int modelToWrite,
      JsonSerializerOptions options) =>
      JsonSerializer.Serialize(writer, modelToWrite, modelToWrite.GetType(), options);
  }
}
