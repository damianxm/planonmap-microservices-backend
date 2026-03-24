using Microsoft.EntityFrameworkCore;
using Session.Shared.Infrastructure;

namespace Session.Features.Sessions.Create;

public static class SessionCodeGenerator
{
    private const int Length = 4;
    private const int MaxAttempts = 100;
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public static async Task<string> GenerateUniqueAsync(SessionDbContext db, CancellationToken ct)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var code = new string(Enumerable.Range(0, Length)
                .Select(_ => Alphabet[Random.Shared.Next(Alphabet.Length)])
                .ToArray());

            if (!await db.Sessions.AnyAsync(s => s.Code == code, ct))
                return code;
        }

        throw new InvalidOperationException($"Unable to generate a unique session code after {MaxAttempts} attempts.");
    }
}
