using System.Security.Cryptography;

namespace CocktailOS.Kiosk.Services;

public sealed class NetworkAccessPinService
{
    private const int SaltLength = 16;
    private const int HashLength = 32;
    private const int Iterations = 120_000;

    public bool IsValid(string? pin) => pin is { Length: 4 } && pin.All(char.IsAsciiDigit);

    public static bool IsHash(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var parts = value.Split('.', StringSplitOptions.None);
        if (parts.Length != 4 || parts[0] != "v1" || !int.TryParse(parts[1], out var iterations) || iterations <= 0) return false;
        try
        {
            return Convert.FromBase64String(parts[2]).Length == SaltLength && Convert.FromBase64String(parts[3]).Length == HashLength;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public string Hash(string pin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pin);
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var hash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, HashAlgorithmName.SHA256, HashLength);
        return $"v1.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string? pin, string? storedHash)
    {
        if (!IsValid(pin) || string.IsNullOrWhiteSpace(storedHash)) return false;
        if (!IsHash(storedHash)) return false;
        var parts = storedHash.Split('.', StringSplitOptions.None);
        var iterations = int.Parse(parts[1]);
        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(pin!, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
