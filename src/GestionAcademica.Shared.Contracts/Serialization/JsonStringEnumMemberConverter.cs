using System.Text.Json;
using System.Text.Json.Serialization;

namespace GestionAcademica.Shared.Contracts.Serialization;

/// <summary>
/// Convertidor JSON que serializa enums como su literal de [EnumMember] (string),
/// cumpliendo la regla de "ENUMs como string" del contrato de la API.
/// </summary>
public sealed class JsonStringEnumMemberConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => (JsonConverter)Activator.CreateInstance(typeof(EnumConverter<>).MakeGenericType(typeToConvert))!;

    private sealed class EnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => EnumHelpers.ToEnum<TEnum>(reader.GetString()!);

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToValue());
    }
}
