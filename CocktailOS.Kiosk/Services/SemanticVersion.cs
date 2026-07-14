namespace CocktailOS.Kiosk.Services;

internal sealed class SemanticVersion : IComparable<SemanticVersion>
{
    private SemanticVersion(int major, int minor, int patch, string[] preRelease)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        PreRelease = preRelease;
    }

    private int Major { get; }
    private int Minor { get; }
    private int Patch { get; }
    private string[] PreRelease { get; }

    public static bool TryParse(string value, out SemanticVersion? version)
    {
        version = null;
        var normalized = value.Trim().TrimStart('v', 'V');
        var buildSeparator = normalized.IndexOf('+');
        if (buildSeparator >= 0) normalized = normalized[..buildSeparator];

        var parts = normalized.Split('-', 2);
        var numbers = parts[0].Split('.');
        if (numbers.Length != 3
            || !int.TryParse(numbers[0], out var major)
            || !int.TryParse(numbers[1], out var minor)
            || !int.TryParse(numbers[2], out var patch)) return false;

        var preRelease = parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1])
            ? parts[1].Split('.', StringSplitOptions.RemoveEmptyEntries)
            : [];
        version = new SemanticVersion(major, minor, patch, preRelease);
        return true;
    }

    public int CompareTo(SemanticVersion? other)
    {
        if (other is null) return 1;
        var numericComparison = Major.CompareTo(other.Major);
        if (numericComparison != 0) return numericComparison;
        numericComparison = Minor.CompareTo(other.Minor);
        if (numericComparison != 0) return numericComparison;
        numericComparison = Patch.CompareTo(other.Patch);
        if (numericComparison != 0) return numericComparison;

        if (PreRelease.Length == 0) return other.PreRelease.Length == 0 ? 0 : 1;
        if (other.PreRelease.Length == 0) return -1;

        for (var index = 0; index < Math.Min(PreRelease.Length, other.PreRelease.Length); index++)
        {
            var left = PreRelease[index];
            var right = other.PreRelease[index];
            var leftIsNumber = int.TryParse(left, out var leftNumber);
            var rightIsNumber = int.TryParse(right, out var rightNumber);
            var comparison = leftIsNumber && rightIsNumber
                ? leftNumber.CompareTo(rightNumber)
                : leftIsNumber ? -1 : rightIsNumber ? 1 : string.CompareOrdinal(left, right);
            if (comparison != 0) return comparison;
        }

        return PreRelease.Length.CompareTo(other.PreRelease.Length);
    }
}
