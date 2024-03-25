using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Distribution.Stream.Converters
{
  public class DoubleConverter : JsonConverter<double>
  {
    public override bool HandleNull => true;

    public override double Read(ref Utf8JsonReader reader, Type dataType, JsonSerializerOptions options)
    {
      if (reader.TokenType is JsonTokenType.Number && reader.TryGetDouble(out var response))
      {
        return response;
      }

      return default;
    }

    public override void Write(
      Utf8JsonWriter writer,
      double modelToWrite,
      JsonSerializerOptions options) =>
      JsonSerializer.Serialize(writer, modelToWrite, modelToWrite.GetType(), options);
  }
}
