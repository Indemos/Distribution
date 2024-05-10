using System;
using System.Text;
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
      JsonSerializerOptions options)
    {
      switch (reader.TokenType)
      {
        case JsonTokenType.Null: return 0;
        case JsonTokenType.Number: return reader.GetDouble();
      }

      return double.TryParse(Encoding.ASCII.GetString(reader.ValueSpan), out var o) ? o : default;
    }

    public override void Write(
      Utf8JsonWriter writer,
      double modelToWrite,
      JsonSerializerOptions options) => writer.WriteStringValue($"{modelToWrite}");
  }
}
