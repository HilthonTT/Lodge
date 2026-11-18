namespace Lodge.Application.Core.Extensions;

/// <summary>
/// Parses enum names from the wire: case-insensitive, defined members only, no numeric strings.
/// </summary>
internal static class EnumNames
{
    public static bool TryParse<TEnum>(string? name, out TEnum value)
        where TEnum : struct, Enum
    {
        value = default;

        if (string.IsNullOrWhiteSpace(name) || char.IsAsciiDigit(name.Trim()[0]) || name.Trim()[0] == '-')
        {
            return false;
        }

        return Enum.TryParse(name.Trim(), ignoreCase: true, out value) && Enum.IsDefined(value);
    }

    public static TEnum Parse<TEnum>(string name)
        where TEnum : struct, Enum =>
        TryParse(name, out TEnum value)
            ? value
            : throw new ArgumentException($"'{name}' is not a valid {typeof(TEnum).Name}.", nameof(name));
}
