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
}
