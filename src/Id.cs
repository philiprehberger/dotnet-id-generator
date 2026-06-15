using System.Security.Cryptography;

namespace Philiprehberger.IdGenerator;

/// <summary>
/// Static factory for generating various types of unique identifiers.
/// </summary>
public static class Id
{
    /// <summary>
    /// Generates a new ULID (Universally Unique Lexicographically Sortable Identifier).
    /// Returns a sortable, 26-character Crockford Base32 encoded value.
    /// </summary>
    /// <returns>A new <see cref="Ulid"/> instance.</returns>
    public static Ulid NewUlid() => new();

    /// <summary>
    /// Generates a new monotonic ULID that is guaranteed to sort strictly after any previous
    /// monotonic ULID generated in the same process, even within the same millisecond.
    /// </summary>
    /// <returns>A new monotonic <see cref="Ulid"/>.</returns>
    public static Ulid NewMonotonicUlid() => Ulid.NewMonotonic();

    /// <summary>
    /// Generates a new NanoID string.
    /// </summary>
    /// <param name="size">The length of the generated ID. Defaults to 21.</param>
    /// <param name="alphabet">
    /// The character set to use. Defaults to the URL-safe alphabet (A-Za-z0-9_-).
    /// </param>
    /// <returns>A randomly generated NanoID string.</returns>
    public static string NewNanoId(int size = 21, string? alphabet = null)
        => NanoId.Generate(size, alphabet);

    /// <summary>
    /// Generates a new Stripe-style prefixed ID (e.g., "usr_01HXYZ...").
    /// </summary>
    /// <param name="prefix">The prefix to prepend (e.g., "usr", "org", "txn").</param>
    /// <returns>A string in the format "{prefix}_{ulid}".</returns>
    public static string NewPrefixed(string prefix)
        => PrefixedId.Create(prefix);

    /// <summary>
    /// Generates a short, URL-safe random ID.
    /// </summary>
    /// <param name="length">The length of the generated ID. Defaults to 12.</param>
    /// <returns>A short random ID string using the URL-safe alphabet.</returns>
    public static string NewShortId(int length = 12)
        => NanoId.Generate(length);

    /// <summary>
    /// Generates a new UUID version 7 (RFC 9562) — a time-ordered UUID with a 48-bit Unix
    /// millisecond timestamp followed by 74 bits of randomness and version/variant nibbles.
    /// </summary>
    /// <returns>A new <see cref="Guid"/> formatted per UUID v7.</returns>
    public static Guid NewGuidV7()
    {
        Span<byte> bytes = stackalloc byte[16];

        var timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        bytes[0] = (byte)(timestampMs >> 40);
        bytes[1] = (byte)(timestampMs >> 32);
        bytes[2] = (byte)(timestampMs >> 24);
        bytes[3] = (byte)(timestampMs >> 16);
        bytes[4] = (byte)(timestampMs >> 8);
        bytes[5] = (byte)timestampMs;

        RandomNumberGenerator.Fill(bytes[6..]);

        // Set version (7) in the upper 4 bits of byte 6
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x70);
        // Set variant (10xxxxxx — RFC 4122 / 9562) in the upper 2 bits of byte 8
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

        // RFC-ordered constructor (big-endian) preserves sort order in string form
        return new Guid(bytes, bigEndian: true);
    }
}
