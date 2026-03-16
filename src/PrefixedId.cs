namespace Philiprehberger.IdGenerator;

/// <summary>
/// Generates and validates Stripe-style prefixed IDs (e.g., "usr_01HXYZ...").
/// </summary>
public static class PrefixedId
{
    /// <summary>
    /// Creates a prefixed ID by combining the given prefix with a ULID.
    /// </summary>
    /// <param name="prefix">The prefix to prepend (e.g., "usr", "org", "txn").</param>
    /// <returns>A string in the format "{prefix}_{ulid}".</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="prefix"/> is null or empty.</exception>
    public static string Create(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException("Prefix must not be null or empty.", nameof(prefix));

        var ulid = new Ulid();
        return $"{prefix}_{ulid}";
    }

    /// <summary>
    /// Validates that an ID starts with the expected prefix and contains a valid ULID.
    /// </summary>
    /// <param name="id">The full prefixed ID string to validate.</param>
    /// <param name="expectedPrefix">The prefix the ID is expected to have.</param>
    /// <returns><c>true</c> if the ID has the expected prefix and a valid ULID; otherwise <c>false</c>.</returns>
    public static bool Validate(string id, string expectedPrefix)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(expectedPrefix))
            return false;

        var prefixWithSeparator = expectedPrefix + "_";
        if (!id.StartsWith(prefixWithSeparator, StringComparison.Ordinal))
            return false;

        var ulidPart = id[prefixWithSeparator.Length..];
        return Ulid.TryParse(ulidPart, out _);
    }
}
