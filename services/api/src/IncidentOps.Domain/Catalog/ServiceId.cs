using System.Text.RegularExpressions;
using IncidentOps.Domain.Common;

namespace IncidentOps.Domain.Catalog;

public sealed partial record ServiceId
{
    public const int MaxLength = 64;

    public static readonly ServiceId Platform = new("platform");

    public ServiceId(string? value)
    {
        var slug = Guard.Required(value, "serviceId", MaxLength);
        Guard.Against(!SlugPattern().IsMatch(slug), "serviceId", $"'{slug}' is not a lowercase slug.");
        Value = slug;
    }

    public string Value { get; }

    public override string ToString() => Value;

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
