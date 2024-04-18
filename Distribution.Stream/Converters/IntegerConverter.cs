using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Distribution.Stream.Converters
{
  public class IntegerConverter : JsonConverter<int>
  {
    public override bool HandleNull => true;

    public override int Read(
      ref Utf8JsonReader reader,
      Type dataType,
      JsonSerializerOptions options) =>
      int.TryParse(reader.GetString(), out var o) ? o : default;

    public override void Write(
      Utf8JsonWriter writer,
      int modelToWrite,
      JsonSerializerOptions options) =>
      JsonSerializer.Serialize(writer, modelToWrite, modelToWrite.GetType(), options);
  }
}
