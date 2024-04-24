using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Distribution.Stream.Converters
{
  public class IntConverter : JsonConverter<int>
  {
    public override bool HandleNull => true;

    public override int Read(
      ref Utf8JsonReader reader,
      Type dataType,
      JsonSerializerOptions options)
    {
      switch (reader.TokenType)
      {
        case JsonTokenType.Null: return 0;
        case JsonTokenType.Number: return reader.GetInt32();
      }

      return int.TryParse(reader.GetString(), out var o) ? o : default;
    }

    public override void Write(
      Utf8JsonWriter writer,
      int modelToWrite,
      JsonSerializerOptions options) =>
      JsonSerializer.Serialize(writer, modelToWrite, modelToWrite.GetType(), options);
  }
}
