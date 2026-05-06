using System;
using System.Globalization;
using Newtonsoft.Json;

namespace Audyssey.MultEQApp;

/// <summary>
/// Reads either a JSON string or a JSON number into a string property; always
/// writes back as a string with invariant-culture formatting.
/// <para>
/// The official Audyssey MultEQ Editor emits <c>customLevel</c>,
/// <c>delayAdjustment</c>, and <c>trimAdjustment</c> as JSON strings (e.g.
/// <c>"-1.5"</c>). The third-party AudysseyOne optimizer
/// (<see href="https://github.com/ObsessiveCompulsiveAudiophile/AudysseyOne"/>)
/// rewrites them as JSON numbers. Accept both on read so users can feed
/// AudysseyOne-processed files back into Ratbuddyssey, but always emit strings
/// so output stays compatible with the official editor.
/// </para>
/// </summary>
internal sealed class NumericStringJsonConverter : JsonConverter<string>
{
    public override string ReadJson(JsonReader reader, Type objectType, string existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.Null:
                return null;
            case JsonToken.String:
                return (string)reader.Value;
            case JsonToken.Integer:
            case JsonToken.Float:
                return Convert.ToString(reader.Value, CultureInfo.InvariantCulture);
            default:
                throw new JsonSerializationException(
                    $"Unexpected token {reader.TokenType} when reading numeric string at path '{reader.Path}'.");
        }
    }

    public override void WriteJson(JsonWriter writer, string value, JsonSerializer serializer)
    {
        if (value == null) writer.WriteNull();
        else writer.WriteValue(value);
    }
}
