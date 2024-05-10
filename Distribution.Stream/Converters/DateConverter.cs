using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Distribution.Stream.Converters
{
  public class DateConverter : JsonConverter<DateTime>
  {
    public override bool HandleNull => true;

    public override DateTime Read(
      ref Utf8JsonReader reader,
      Type dataType,
      JsonSerializerOptions options) =>
      DateTime.TryParse(Encoding.ASCII.GetString(reader.ValueSpan), out var o) ? o : default;

    public override void Write(
      Utf8JsonWriter writer,
      DateTime modelToWrite,
      JsonSerializerOptions options) => writer.WriteStringValue($"{modelToWrite}");
  }
}
