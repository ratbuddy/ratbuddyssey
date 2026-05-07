using System;
using System.Globalization;
using Newtonsoft.Json;

namespace Audyssey.MultEQApp;

/// <summary>
/// Reads either a JSON string or a JSON number into a string property; writes
/// back as a JSON string.
/// <para>
/// The official Audyssey MultEQ Editor emits <c>customLevel</c>,
/// <c>delayAdjustment</c>, and <c>trimAdjustment</c> as JSON strings (e.g.
/// <c>"-1.5"</c>). The third-party AudysseyOne optimizer
/// (<see href="https://github.com/ObsessiveCompulsiveAudiophile/AudysseyOne"/>)
/// rewrites them as JSON numbers. Accept both on read so users can feed
/// AudysseyOne-processed files back into Ratbuddyssey, but always emit strings
/// so output stays compatible with the official editor.
/// </para>
/// <para>
/// On write the stored string is preserved verbatim so a round-trip of an
/// untouched file is byte-for-byte identical, even if the user's locale
/// formats decimals differently. The integer / float read paths use
/// <see cref="CultureInfo.InvariantCulture"/> to construct the string in the
/// first place, so culture leakage can only occur if a caller assigns a
/// locale-formatted string directly to the property — which the rest of the
/// app does not do.
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
