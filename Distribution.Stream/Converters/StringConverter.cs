using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Distribution.Stream.Converters
{
  public class StringConverter : JsonConverter<string>
  {
    public override bool HandleNull => true;

    public override string Read(
      ref Utf8JsonReader reader,
      Type dataType,
      JsonSerializerOptions options) => Encoding.Default.GetString(reader.ValueSpan);

    public override void Write(
      Utf8JsonWriter writer,
      string modelToWrite,
      JsonSerializerOptions options) =>
      JsonSerializer.Serialize(writer, modelToWrite, modelToWrite.GetType(), options);
  }
}
