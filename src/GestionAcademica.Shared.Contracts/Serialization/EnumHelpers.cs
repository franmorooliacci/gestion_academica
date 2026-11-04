using System.Reflection;
using System.Runtime.Serialization;

namespace GestionAcademica.Shared.Contracts.Serialization;

/// <summary>
/// Convierte enums a/desde sus literales de base de datos/JSON usando [EnumMember].
/// Permite mapear valores con caracteres no-ASCII (ej. 'mañana') sin problemas.
/// </summary>
public static class EnumHelpers
{
    public static string ToValue<TEnum>(this TEnum value) where TEnum : struct, Enum
    {
        var member = typeof(TEnum).GetMember(value.ToString())[0];
        var attr = member.GetCustomAttribute<EnumMemberAttribute>();
        return attr?.Value ?? value.ToString();
    }

    public static TEnum ToEnum<TEnum>(string value) where TEnum : struct, Enum
    {
        foreach (var field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var attr = field.GetCustomAttribute<EnumMemberAttribute>();
            if (string.Equals(attr?.Value, value, StringComparison.OrdinalIgnoreCase)
                || string.Equals(field.Name, value, StringComparison.OrdinalIgnoreCase))
            {
                return (TEnum)field.GetValue(null)!;
            }
        }

        throw new ArgumentException($"'{value}' no es un valor válido para {typeof(TEnum).Name}.");
    }
}
