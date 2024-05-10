using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Distribution.Stream.Converters
{
  public class BoolConverter : JsonConverter<bool>
  {
    public override bool HandleNull => true;

    public override bool Read(
      ref Utf8JsonReader reader,
      Type dataType,
      JsonSerializerOptions options)
    {
      switch (reader.TokenType)
      {
        case JsonTokenType.Null: 
        case JsonTokenType.False: return false;
        case JsonTokenType.True: return true;
      }

      return bool.TryParse(Encoding.ASCII.GetString(reader.ValueSpan), out var o) && o;
    }

    public override void Write(
      Utf8JsonWriter writer,
      bool modelToWrite,
      JsonSerializerOptions options) => writer.WriteStringValue($"{modelToWrite}");
  }
}
