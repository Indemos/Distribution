using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Distribution.Stream.Converters
{
  public class DoubleConverter : JsonConverter<double>
  {
    public override bool HandleNull => true;

    public override double Read(
      ref Utf8JsonReader reader,
      Type dataType,
      JsonSerializerOptions options) =>
      double.TryParse(reader.GetString(), out var o) ? o : default;

    public override void Write(
      Utf8JsonWriter writer,
      double modelToWrite,
      JsonSerializerOptions options) =>
      JsonSerializer.Serialize(writer, modelToWrite, modelToWrite.GetType(), options);
  }
}
