using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace CocktailOS.Kiosk.Services;

public sealed class NetworkAccessSessionService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(12);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _sessions = new();

    public string Create()
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _sessions[token] = DateTimeOffset.UtcNow.Add(Lifetime);
        return token;
    }

    public bool IsValid(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || !_sessions.TryGetValue(token, out var expiresAt)) return false;
        if (expiresAt > DateTimeOffset.UtcNow) return true;
        _sessions.TryRemove(token, out _);
        return false;
    }
}
